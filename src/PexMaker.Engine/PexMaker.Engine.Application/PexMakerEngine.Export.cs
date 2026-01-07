using System.Linq;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;
using PexMaker.Engine.Domain.Units;

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

        var resolution = ResolutionDpi.Create(normalizedProject.Dpi.Value);
        var cardWidthPx = UnitConverter.ToPixels((decimal)metrics.CardWidthMm, Unit.Millimeter, resolution, PxRounding.Round);
        var cardHeightPx = UnitConverter.ToPixels((decimal)metrics.CardHeightMm, Unit.Millimeter, resolution, PxRounding.Round);
        var borderPx = UnitConverter.ToPixels((decimal)normalizedProject.Layout.BorderThickness.Value, Unit.Millimeter, resolution, PxRounding.Round);
        var cornerPx = UnitConverter.ToPixels((decimal)normalizedProject.Layout.CornerRadius.Value, Unit.Millimeter, resolution, PxRounding.Round);
        var bleedPx = UnitConverter.ToPixels((decimal)normalizedProject.Layout.Bleed.Value, Unit.Millimeter, resolution, PxRounding.Round);
        var cutMarkLengthMm = normalizedProject.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(normalizedProject.Layout.CutMarkLength, EngineDefaults.DefaultCutMarkLength)
            : Mm.Zero;
        var cutMarkThicknessMm = normalizedProject.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(normalizedProject.Layout.CutMarkThickness, EngineDefaults.DefaultCutMarkThickness)
            : Mm.Zero;
        var cutMarkOffsetMm = normalizedProject.Layout.CutMarks
            ? NormalizeCutMarkMeasurement(normalizedProject.Layout.CutMarkOffset, EngineDefaults.DefaultCutMarkOffset)
            : Mm.Zero;
        var cutMarkLengthPx = UnitConverter.ToPixels((decimal)cutMarkLengthMm.Value, Unit.Millimeter, resolution, PxRounding.Round);
        var cutMarkThicknessPx = UnitConverter.ToPixels((decimal)cutMarkThicknessMm.Value, Unit.Millimeter, resolution, PxRounding.Round);
        var cutMarkOffsetPx = UnitConverter.ToPixels((decimal)cutMarkOffsetMm.Value, Unit.Millimeter, resolution, PxRounding.Round);

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
