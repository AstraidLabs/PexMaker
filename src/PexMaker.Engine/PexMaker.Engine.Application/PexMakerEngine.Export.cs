using System.Linq;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    public async Task<ExportResult> ExportAsync(PexProject project, ExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = await ValidateAsync(project, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return new ExportResult(false, Array.Empty<string>(), validation);
        }

        var formatResult = ResolveFormat(request.Format);
        if (formatResult.Error is not null)
        {
            var invalidResult = new ProjectValidationResult(new[] { formatResult.Error }, validation.Warnings);
            return new ExportResult(false, Array.Empty<string>(), invalidResult);
        }

        var deck = await BuildDeckAsync(project, request.Seed, cancellationToken).ConfigureAwait(false);
        var metrics = LayoutCalculator.Calculate(project.Layout);
        var layout = BuildLayoutPlan(project, deck, metrics);

        var frontPages = (int)Math.Ceiling(deck.CardCount / (double)layout.Grid.PerPage);
        var totalSheets = layout.Pages.Count(page => (page.Side == SheetSide.Front && request.IncludeFront)
            || (page.Side == SheetSide.Back && request.IncludeBack));

        var safetyErrors = new List<ValidationError>();
        if (request.IncludeFront && frontPages > EngineLimits.MaxPages)
        {
            safetyErrors.Add(new ValidationError(EngineErrorCode.ExceedsPageLimit, $"Page count {frontPages} exceeds limit {EngineLimits.MaxPages}", nameof(project.Layout)));
        }

        if (totalSheets > EngineLimits.MaxOutputFiles)
        {
            safetyErrors.Add(new ValidationError(EngineErrorCode.ExceedsOutputLimit, $"Output file count {totalSheets} exceeds limit {EngineLimits.MaxOutputFiles}", nameof(request.OutputDirectory)));
        }

        if (safetyErrors.Count > 0)
        {
            var invalidResult = new ProjectValidationResult(validation.Errors.Concat(safetyErrors), validation.Warnings);
            return new ExportResult(false, Array.Empty<string>(), invalidResult);
        }

        var cardWidthPx = Units.MmToPx(new Mm(metrics.CardWidthMm), project.Dpi);
        var cardHeightPx = Units.MmToPx(new Mm(metrics.CardHeightMm), project.Dpi);
        var borderPx = Units.MmToPx(project.Layout.BorderThickness, project.Dpi);
        var cornerPx = Units.MmToPx(project.Layout.CornerRadius, project.Dpi);
        var bleedPx = Units.MmToPx(project.Layout.Bleed, project.Dpi);
        var cutMarkLengthMm = project.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(project.Layout.CutMarkLength, EngineDefaults.DefaultCutMarkLength)
            : Mm.Zero;
        var cutMarkThicknessMm = project.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(project.Layout.CutMarkThickness, EngineDefaults.DefaultCutMarkThickness)
            : Mm.Zero;
        var cutMarkOffsetMm = project.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(project.Layout.CutMarkOffset, EngineDefaults.DefaultCutMarkOffset)
            : Mm.Zero;
        var cutMarkLengthPx = Units.MmToPx(cutMarkLengthMm, project.Dpi);
        var cutMarkThicknessPx = Units.MmToPx(cutMarkThicknessMm, project.Dpi);
        var cutMarkOffsetPx = Units.MmToPx(cutMarkOffsetMm, project.Dpi);

        _fileSystem.CreateDirectory(request.OutputDirectory);

        var exportRequest = new SheetExportRequest
        {
            OutputDirectory = request.OutputDirectory,
            NamingPrefixFront = request.NamingPrefixFront,
            NamingPrefixBack = request.NamingPrefixBack,
            Format = formatResult.Format,
            Layout = layout,
            Cards = deck.Cards,
            BackImage = deck.Back,
            IncludeFront = request.IncludeFront,
            IncludeBack = request.IncludeBack,
            IncludeCutMarks = project.Layout.CutMarks,
            CutMarkLengthPx = cutMarkLengthPx,
            CutMarkThicknessPx = cutMarkThicknessPx,
            CutMarkOffsetPx = cutMarkOffsetPx,
            BorderEnabled = project.Layout.BorderEnabled,
            BorderThicknessPx = borderPx,
            CornerRadiusPx = cornerPx,
            CardWidthPx = cardWidthPx,
            CardHeightPx = cardHeightPx,
            BleedPx = bleedPx,
            EnableParallelism = request.EnableParallelism,
            MaxDegreeOfParallelism = Math.Max(1, request.MaxDegreeOfParallelism),
            MaxBufferedPages = Math.Max(1, request.MaxBufferedPages),
            MaxBufferedCards = Math.Max(1, request.MaxBufferedCards),
            MaxCacheItems = Math.Max(1, request.MaxCacheItems),
            MaxEstimatedWorkingSetBytes = request.MaxEstimatedWorkingSetBytes,
            Progress = request.Progress,
        };

        var exportResult = await _sheetExporter.ExportAsync(exportRequest, cancellationToken).ConfigureAwait(false);
        var combinedWarnings = validation.Warnings.Concat(exportResult.ValidationResult.Warnings).ToArray();
        var combinedErrors = exportResult.ValidationResult.Errors;
        var combinedResult = new ProjectValidationResult(combinedErrors, combinedWarnings);
        return new ExportResult(exportResult.IsSuccess, exportResult.Files, combinedResult);
    }

    private static Mm NormalizeCutMarkMeasurement(Mm value, Mm fallback)
    {
        return value.Value > 0 ? value : fallback;
    }

    private static (ExportImageFormat Format, ValidationError? Error) ResolveFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format) || string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
        {
            return (ExportImageFormat.Png, null);
        }

        var error = new ValidationError(EngineErrorCode.UnsupportedFormat, $"Format '{format}' is not supported. Only 'png' is allowed.", nameof(ExportRequest.Format));
        return (ExportImageFormat.Png, error);
    }
}
