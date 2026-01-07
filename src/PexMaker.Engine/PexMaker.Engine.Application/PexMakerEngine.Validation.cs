using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    public Task<ProjectValidationResult> ValidateAsync(PexProject project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        if (project.PairCount < 1 || project.PairCount > EngineLimits.MaxPairs)
        {
            errors.Add(new ValidationError(EngineErrorCode.InvalidPairCount, $"Pair count must be between 1 and {EngineLimits.MaxPairs}", nameof(project.PairCount)));
        }

        if (project.FrontImages is null || project.FrontImages.Count < project.PairCount)
        {
            errors.Add(new ValidationError(EngineErrorCode.InsufficientFrontImages, $"At least {project.PairCount} front images are required", nameof(project.FrontImages)));
        }
        else if (project.FrontImages.Count > EngineLimits.MaxImagesPerProject)
        {
            warnings.Add(new ValidationError(EngineErrorCode.InvalidMeasurement, $"Front image count exceeds the suggested maximum of {EngineLimits.MaxImagesPerProject}", nameof(project.FrontImages)));
        }

        if (project.BackImage is null)
        {
            errors.Add(new ValidationError(EngineErrorCode.MissingBackImage, "Back image is required", nameof(project.BackImage)));
        }

        if (project.Dpi.Value < EngineLimits.MinDpi || project.Dpi.Value > EngineLimits.MaxDpi)
        {
            errors.Add(new ValidationError(EngineErrorCode.InvalidDpi, $"DPI must be between {EngineLimits.MinDpi} and {EngineLimits.MaxDpi}", nameof(project.Dpi)));
        }

        ValidateMeasurements(project, errors);

        var layout = project.Layout ?? new LayoutOptions();
        if (layout.Bleed.Value > 0)
        {
            if (layout.Bleed.Value * 2 > layout.Gutter.Value)
            {
                warnings.Add(new ValidationError(
                    EngineErrorCode.InvalidMeasurement,
                    "Bleed is larger than half the gutter, so card bleeds may overlap.",
                    nameof(layout.Bleed)));
            }

            var minMargin = Math.Min(
                Math.Min(layout.MarginLeft.Value, layout.MarginRight.Value),
                Math.Min(layout.MarginTop.Value, layout.MarginBottom.Value));
            if (layout.Bleed.Value > minMargin)
            {
                warnings.Add(new ValidationError(
                    EngineErrorCode.InvalidMeasurement,
                    "Bleed exceeds the configured margins and may be clipped at the page edge.",
                    nameof(layout.Bleed)));
            }
        }

        var metrics = LayoutCalculator.Calculate(project.Layout);
        if (project.Layout.AutoFitMode != AutoFitMode.None && project.Layout.Grid is null)
        {
            errors.Add(new ValidationError(EngineErrorCode.InvalidMeasurement, "Auto-fit requires grid specification.", nameof(project.Layout.Grid)));
        }
        else if (project.Layout.AutoFitMode != AutoFitMode.None && project.Layout.Grid is not null
            && (project.Layout.Grid.Columns <= 0 || project.Layout.Grid.Rows <= 0))
        {
            errors.Add(new ValidationError(EngineErrorCode.InvalidMeasurement, "Grid columns and rows must be positive.", nameof(project.Layout.Grid)));
        }

        if (metrics.PerPage < 1 || metrics.CardWidthMm <= 0 || metrics.CardHeightMm <= 0)
        {
            errors.Add(new ValidationError(EngineErrorCode.LayoutDoesNotFit, "No cards fit on the configured page", nameof(project.Layout)));
        }

        return Task.FromResult(new ProjectValidationResult(errors, warnings));
    }

    private static void ValidateMeasurements(PexProject project, ICollection<ValidationError> errors)
    {
        var layout = project.Layout ?? new LayoutOptions();
        var measurements = new (Mm Value, string Path)[]
        {
            (layout.MarginLeft, nameof(layout.MarginLeft)),
            (layout.MarginTop, nameof(layout.MarginTop)),
            (layout.MarginRight, nameof(layout.MarginRight)),
            (layout.MarginBottom, nameof(layout.MarginBottom)),
            (layout.Gutter, nameof(layout.Gutter)),
            (layout.CardWidth, nameof(layout.CardWidth)),
            (layout.CardHeight, nameof(layout.CardHeight)),
            (layout.CornerRadius, nameof(layout.CornerRadius)),
            (layout.BorderThickness, nameof(layout.BorderThickness)),
            (layout.Bleed, nameof(layout.Bleed)),
        };

        foreach (var measurement in measurements)
        {
            if (!measurement.Value.IsFinite || measurement.Value.Value < EngineLimits.MinMm || measurement.Value.Value > EngineLimits.MaxMm)
            {
                errors.Add(new ValidationError(EngineErrorCode.InvalidMeasurement, $"Measurement {measurement.Path} must be finite and between {EngineLimits.MinMm} and {EngineLimits.MaxMm} mm", measurement.Path));
            }
        }
    }

    private static (double WidthMm, double HeightMm) ResolvePageSize(LayoutOptions layout) => PageSizeResolver.ResolvePageSize(layout);
}
