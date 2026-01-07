using System.Linq;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    /// <summary>
    /// Exports the project after validating inputs and output paths. Existing output files cause validation errors.
    /// </summary>
    public async Task<ExportResult> ExportAsync(PexProject project, ExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var projectValidation = ValidateProject(project, out var normalizedProject);
        var requestValidation = ValidateExportRequest(request, out var normalizedRequest);
        var combinedValidation = ProjectValidationResult.Combine(projectValidation, requestValidation);
        if (!combinedValidation.IsValid)
        {
            return new ExportResult(false, Array.Empty<string>(), combinedValidation);
        }

        var deck = BuildShuffledDeck(normalizedProject, normalizedRequest.Seed);
        var metrics = LayoutCalculator.Calculate(normalizedProject.Layout);
        var layout = BuildLayoutPlan(normalizedProject, deck, metrics);

        var frontPages = (int)Math.Ceiling(deck.CardCount / (double)layout.Grid.PerPage);
        var totalSheets = layout.Pages.Count(page => (page.Side == SheetSide.Front && normalizedRequest.IncludeFront)
            || (page.Side == SheetSide.Back && normalizedRequest.IncludeBack));

        var safetyErrors = new List<ValidationError>();
        if (normalizedRequest.IncludeFront && frontPages > EngineLimits.MaxPages)
        {
            safetyErrors.Add(new ValidationError(EngineErrorCode.ExceedsPageLimit, $"Page count {frontPages} exceeds limit {EngineLimits.MaxPages}", nameof(project.Layout)));
        }

        if (totalSheets > EngineLimits.MaxOutputFiles)
        {
            safetyErrors.Add(new ValidationError(EngineErrorCode.ExceedsOutputLimit, $"Output file count {totalSheets} exceeds limit {EngineLimits.MaxOutputFiles}", nameof(request.OutputDirectory)));
        }

        var pathValidation = ValidateExportPaths(layout, normalizedRequest);
        var exportValidation = safetyErrors.Count > 0
            ? ProjectValidationResult.Combine(combinedValidation, new ProjectValidationResult(safetyErrors, null), pathValidation)
            : ProjectValidationResult.Combine(combinedValidation, pathValidation);

        if (!exportValidation.IsValid)
        {
            return new ExportResult(false, Array.Empty<string>(), exportValidation);
        }

        var cardWidthPx = Units.MmToPx(new Mm(metrics.CardWidthMm), normalizedProject.Dpi);
        var cardHeightPx = Units.MmToPx(new Mm(metrics.CardHeightMm), normalizedProject.Dpi);
        var borderPx = Units.MmToPx(normalizedProject.Layout.BorderThickness, normalizedProject.Dpi);
        var cornerPx = Units.MmToPx(normalizedProject.Layout.CornerRadius, normalizedProject.Dpi);
        var bleedPx = Units.MmToPx(normalizedProject.Layout.Bleed, normalizedProject.Dpi);
        var cutMarkLengthMm = normalizedProject.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(normalizedProject.Layout.CutMarkLength, EngineDefaults.DefaultCutMarkLength)
            : Mm.Zero;
        var cutMarkThicknessMm = normalizedProject.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(normalizedProject.Layout.CutMarkThickness, EngineDefaults.DefaultCutMarkThickness)
            : Mm.Zero;
        var cutMarkOffsetMm = normalizedProject.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(normalizedProject.Layout.CutMarkOffset, EngineDefaults.DefaultCutMarkOffset)
            : Mm.Zero;
        var cutMarkLengthPx = Units.MmToPx(cutMarkLengthMm, normalizedProject.Dpi);
        var cutMarkThicknessPx = Units.MmToPx(cutMarkThicknessMm, normalizedProject.Dpi);
        var cutMarkOffsetPx = Units.MmToPx(cutMarkOffsetMm, normalizedProject.Dpi);

        var exportRequest = new SheetExportRequest
        {
            OutputDirectory = normalizedRequest.OutputDirectory,
            NamingPrefixFront = normalizedRequest.NamingPrefixFront,
            NamingPrefixBack = normalizedRequest.NamingPrefixBack,
            Format = ResolveFormat(normalizedRequest.Format),
            Layout = layout,
            Cards = deck.Cards,
            BackImage = deck.Back,
            IncludeFront = normalizedRequest.IncludeFront,
            IncludeBack = normalizedRequest.IncludeBack,
            IncludeCutMarks = normalizedProject.Layout.CutMarks,
            CutMarkLengthPx = cutMarkLengthPx,
            CutMarkThicknessPx = cutMarkThicknessPx,
            CutMarkOffsetPx = cutMarkOffsetPx,
            BorderEnabled = normalizedProject.Layout.BorderEnabled,
            BorderThicknessPx = borderPx,
            CornerRadiusPx = cornerPx,
            CardWidthPx = cardWidthPx,
            CardHeightPx = cardHeightPx,
            BleedPx = bleedPx,
            EnableParallelism = normalizedRequest.EnableParallelism,
            MaxDegreeOfParallelism = Math.Max(1, normalizedRequest.MaxDegreeOfParallelism),
            MaxBufferedPages = Math.Max(1, normalizedRequest.MaxBufferedPages),
            MaxBufferedCards = Math.Max(1, normalizedRequest.MaxBufferedCards),
            MaxCacheItems = Math.Max(1, normalizedRequest.MaxCacheItems),
            MaxEstimatedWorkingSetBytes = normalizedRequest.MaxEstimatedWorkingSetBytes,
            Progress = normalizedRequest.Progress,
        };

        var exportResult = await _sheetExporter.ExportAsync(exportRequest, cancellationToken).ConfigureAwait(false);
        var combinedWarnings = exportValidation.Warnings.Concat(exportResult.ValidationResult.Warnings).ToArray();
        var combinedErrors = exportValidation.Errors.Concat(exportResult.ValidationResult.Errors).ToArray();
        var combinedResult = new ProjectValidationResult(combinedErrors, combinedWarnings);
        return new ExportResult(exportResult.IsSuccess, exportResult.Files, combinedResult);
    }

    private static Mm NormalizeCutMarkMeasurement(Mm value, Mm fallback)
    {
        return value.Value > 0 ? value : fallback;
    }

    private static ExportImageFormat ResolveFormat(string? format)
        => string.Equals(format, "png", StringComparison.OrdinalIgnoreCase)
            ? ExportImageFormat.Png
            : ExportImageFormat.Png;
}
