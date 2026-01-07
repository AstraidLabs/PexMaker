using System;
using System.Collections.Generic;
using System.IO;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal sealed class EngineInputValidator
{
    private static readonly char[] DisallowedPrefixChars = ['/', '\\'];
    private readonly IFileSystem _fileSystem;

    public EngineInputValidator(IFileSystem fileSystem)
    {
        _fileSystem = Guard.NotNull(fileSystem, nameof(fileSystem));
    }

    public ProjectValidationResult ValidateProject(PexProject project, out PexProject normalizedProject)
    {
        ArgumentNullException.ThrowIfNull(project);

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        if (project.PairCount < 1 || project.PairCount > EngineLimits.MaxPairs)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidPairCount,
                $"Pair count must be between 1 and {EngineLimits.MaxPairs}.",
                nameof(PexProject.PairCount)));
        }

        var normalizedFrontImages = new List<ImageRef>();
        if (project.FrontImages is null)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidFrontImages,
                "Front images are required.",
                nameof(PexProject.FrontImages)));
        }
        else
        {
            if (project.FrontImages.Count < project.PairCount)
            {
                errors.Add(new ValidationError(
                    EngineErrorCode.InsufficientFrontImages,
                    $"At least {project.PairCount} front images are required.",
                    nameof(PexProject.FrontImages)));
            }

            if (project.FrontImages.Count > EngineLimits.MaxImagesPerProject)
            {
                warnings.Add(new ValidationError(
                    EngineErrorCode.InvalidMeasurement,
                    $"Front image count exceeds the suggested maximum of {EngineLimits.MaxImagesPerProject}.",
                    nameof(PexProject.FrontImages),
                    ValidationSeverity.Warning));
            }

            for (var i = 0; i < project.FrontImages.Count; i++)
            {
                var imagePath = $"{nameof(PexProject.FrontImages)}[{i}]";
                var normalizedImage = NormalizeImage(project.FrontImages[i], imagePath, errors);
                if (normalizedImage is not null)
                {
                    normalizedFrontImages.Add(normalizedImage);
                }
            }
        }

        var normalizedBackImage = project.BackImage is null
            ? null
            : NormalizeImage(project.BackImage, nameof(PexProject.BackImage), errors);
        if (project.BackImage is null)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.MissingBackImage,
                "Back image is required.",
                nameof(PexProject.BackImage)));
        }

        if (project.Dpi.Value < EngineLimits.MinDpi || project.Dpi.Value > EngineLimits.MaxDpi)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidDpi,
                $"DPI must be between {EngineLimits.MinDpi} and {EngineLimits.MaxDpi}.",
                nameof(PexProject.Dpi)));
        }

        var layout = project.Layout ?? new LayoutOptions();
        ValidateLayout(layout, project.Dpi, errors, warnings);

        normalizedProject = new PexProject
        {
            PairCount = project.PairCount,
            FrontImages = normalizedFrontImages,
            BackImage = normalizedBackImage,
            Layout = layout,
            Dpi = project.Dpi,
        };

        return new ProjectValidationResult(errors, warnings);
    }

    public ProjectValidationResult ValidateExportRequest(ExportRequest request, out ExportRequest normalizedRequest)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        var outputDirectory = NormalizeDirectory(request.OutputDirectory, errors, nameof(ExportRequest.OutputDirectory));
        if (!string.IsNullOrWhiteSpace(outputDirectory) && _fileSystem.FileExists(outputDirectory))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidOutputDirectory,
                "Output directory points to a file.",
                nameof(ExportRequest.OutputDirectory)));
        }
        else if (!string.IsNullOrWhiteSpace(outputDirectory) && !_fileSystem.DirectoryExists(outputDirectory))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.OutputDirectoryMissing,
                "Output directory does not exist. The engine does not create output folders automatically.",
                nameof(ExportRequest.OutputDirectory)));
        }

        var prefixFront = NormalizePrefix(request.NamingPrefixFront, errors, nameof(ExportRequest.NamingPrefixFront));
        var prefixBack = NormalizePrefix(request.NamingPrefixBack, errors, nameof(ExportRequest.NamingPrefixBack));

        if (!request.IncludeFront && !request.IncludeBack)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidExportSelection,
                "At least one of IncludeFront or IncludeBack must be true.",
                nameof(ExportRequest)));
        }

        if (request.MaxDegreeOfParallelism < EngineLimits.MinParallelism
            || request.MaxDegreeOfParallelism > EngineLimits.MaxParallelism)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidParallelism,
                $"MaxDegreeOfParallelism must be between {EngineLimits.MinParallelism} and {EngineLimits.MaxParallelism}.",
                nameof(ExportRequest.MaxDegreeOfParallelism)));
        }

        if (request.MaxBufferedPages < EngineLimits.MinBufferedPages
            || request.MaxBufferedPages > EngineLimits.MaxBufferedPages)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidBufferLimit,
                $"MaxBufferedPages must be between {EngineLimits.MinBufferedPages} and {EngineLimits.MaxBufferedPages}.",
                nameof(ExportRequest.MaxBufferedPages)));
        }

        if (request.MaxBufferedCards < EngineLimits.MinBufferedCards
            || request.MaxBufferedCards > EngineLimits.MaxBufferedCards)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidBufferLimit,
                $"MaxBufferedCards must be between {EngineLimits.MinBufferedCards} and {EngineLimits.MaxBufferedCards}.",
                nameof(ExportRequest.MaxBufferedCards)));
        }

        if (request.MaxCacheItems < EngineLimits.MinCacheItems
            || request.MaxCacheItems > EngineLimits.MaxCacheItems)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidCacheLimit,
                $"MaxCacheItems must be between {EngineLimits.MinCacheItems} and {EngineLimits.MaxCacheItems}.",
                nameof(ExportRequest.MaxCacheItems)));
        }

        if (request.MaxEstimatedWorkingSetBytes < EngineLimits.MinEstimatedWorkingSetBytes
            || request.MaxEstimatedWorkingSetBytes > EngineLimits.MaxEstimatedWorkingSetBytes)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidWorkingSetLimit,
                $"MaxEstimatedWorkingSetBytes must be between {EngineLimits.MinEstimatedWorkingSetBytes:N0} and {EngineLimits.MaxEstimatedWorkingSetBytes:N0} bytes.",
                nameof(ExportRequest.MaxEstimatedWorkingSetBytes)));
        }

        var formatResult = NormalizeFormat(request.Format, errors);

        normalizedRequest = new ExportRequest
        {
            OutputDirectory = outputDirectory ?? request.OutputDirectory,
            NamingPrefixFront = prefixFront ?? request.NamingPrefixFront,
            NamingPrefixBack = prefixBack ?? request.NamingPrefixBack,
            Format = formatResult,
            Seed = request.Seed ?? 0,
            IncludeFront = request.IncludeFront,
            IncludeBack = request.IncludeBack,
            EnableParallelism = request.EnableParallelism,
            MaxDegreeOfParallelism = request.MaxDegreeOfParallelism,
            MaxBufferedPages = request.MaxBufferedPages,
            MaxBufferedCards = request.MaxBufferedCards,
            MaxCacheItems = request.MaxCacheItems,
            MaxEstimatedWorkingSetBytes = request.MaxEstimatedWorkingSetBytes,
            Progress = request.Progress,
        };

        return new ProjectValidationResult(errors, warnings);
    }

    public ProjectValidationResult ValidateExportPaths(LayoutPlan layout, ExportRequest request)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        var outputDirectory = request.OutputDirectory;
        var outputRoot = Path.GetFullPath(outputDirectory);
        var comparer = StringComparer.OrdinalIgnoreCase;
        var knownPaths = new HashSet<string>(comparer);

        foreach (var page in layout.Pages)
        {
            if (page.Side == SheetSide.Front && !request.IncludeFront)
            {
                continue;
            }

            if (page.Side == SheetSide.Back && !request.IncludeBack)
            {
                continue;
            }

            var prefix = page.Side == SheetSide.Front ? request.NamingPrefixFront : request.NamingPrefixBack;
            var fileName = Naming.PageFileName(prefix, page.PageNumber, ResolveFormat(request.Format));
            var outputPath = Path.Combine(outputDirectory, fileName);
            var fullPath = Path.GetFullPath(outputPath);

            if (!IsWithinDirectory(outputRoot, fullPath))
            {
                errors.Add(new ValidationError(
                    EngineErrorCode.InvalidOutputDirectory,
                    "Generated output path is outside the output directory.",
                    nameof(ExportRequest.OutputDirectory)));
                continue;
            }

            if (!knownPaths.Add(fullPath))
            {
                errors.Add(new ValidationError(
                    EngineErrorCode.NamingCollision,
                    $"Output file '{fileName}' would be generated multiple times.",
                    nameof(ExportRequest)));
                continue;
            }

            if (_fileSystem.FileExists(fullPath))
            {
                errors.Add(new ValidationError(
                    EngineErrorCode.OutputFileExists,
                    $"Output file '{fileName}' already exists.",
                    nameof(ExportRequest.OutputDirectory)));
            }
        }

        return new ProjectValidationResult(errors, warnings);
    }

    private void ValidateLayout(LayoutOptions layout, Dpi dpi, ICollection<ValidationError> errors, ICollection<ValidationError> warnings)
    {
        if (!Enum.IsDefined(layout.Format))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidPageFormat,
                "Page format is invalid.",
                nameof(LayoutOptions.Format)));
        }

        if (!Enum.IsDefined(layout.Orientation))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidPageOrientation,
                "Page orientation is invalid.",
                nameof(LayoutOptions.Orientation)));
        }

        if (!Enum.IsDefined(layout.DuplexMode))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidDuplexMode,
                "Duplex mode is invalid.",
                nameof(LayoutOptions.DuplexMode)));
        }

        if (!Enum.IsDefined(layout.Alignment))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidAlignment,
                "Grid alignment is invalid.",
                nameof(LayoutOptions.Alignment)));
        }

        if (!Enum.IsDefined(layout.AutoFitMode))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidAutoFitMode,
                "Auto-fit mode is invalid.",
                nameof(LayoutOptions.AutoFitMode)));
        }

        ValidateMeasurement(layout.MarginLeft, nameof(LayoutOptions.MarginLeft), errors);
        ValidateMeasurement(layout.MarginTop, nameof(LayoutOptions.MarginTop), errors);
        ValidateMeasurement(layout.MarginRight, nameof(LayoutOptions.MarginRight), errors);
        ValidateMeasurement(layout.MarginBottom, nameof(LayoutOptions.MarginBottom), errors);
        ValidateMeasurement(layout.Gutter, nameof(LayoutOptions.Gutter), errors);
        ValidateMeasurement(layout.CardWidth, nameof(LayoutOptions.CardWidth), errors);
        ValidateMeasurement(layout.CardHeight, nameof(LayoutOptions.CardHeight), errors);
        ValidateMeasurement(layout.CornerRadius, nameof(LayoutOptions.CornerRadius), errors);
        ValidateMeasurement(layout.BorderThickness, nameof(LayoutOptions.BorderThickness), errors);

        ValidateMeasurement(layout.Bleed, nameof(LayoutOptions.Bleed), errors, max: EngineLimits.MaxBleedMm);
        if (layout.Bleed.Value < 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidBleed,
                "Bleed must be zero or positive.",
                nameof(LayoutOptions.Bleed)));
        }

        if (layout.CardWidth.Value <= 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidCardSize,
                "Card width must be positive.",
                nameof(LayoutOptions.CardWidth)));
        }

        if (layout.CardHeight.Value <= 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidCardSize,
                "Card height must be positive.",
                nameof(LayoutOptions.CardHeight)));
        }

        if (layout.CutMarks)
        {
            ValidateMeasurement(layout.CutMarkLength, nameof(LayoutOptions.CutMarkLength), errors, max: EngineLimits.MaxCutMarkLengthMm);
            ValidateMeasurement(layout.CutMarkThickness, nameof(LayoutOptions.CutMarkThickness), errors, max: EngineLimits.MaxCutMarkThicknessMm);
            ValidateMeasurement(layout.CutMarkOffset, nameof(LayoutOptions.CutMarkOffset), errors, max: EngineLimits.MaxCutMarkOffsetMm);

            if (layout.CutMarkLength.Value <= 0 || layout.CutMarkThickness.Value <= 0)
            {
                errors.Add(new ValidationError(
                    EngineErrorCode.InvalidCutMarks,
                    "Cut mark length and thickness must be positive.",
                    nameof(LayoutOptions.CutMarks)));
            }
        }

        if (layout.AutoFitMode != AutoFitMode.None && layout.Grid is null)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidGrid,
                "Auto-fit requires a grid specification.",
                nameof(LayoutOptions.Grid)));
        }
        else if (layout.Grid is not null && (layout.Grid.Columns <= 0 || layout.Grid.Rows <= 0))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidGrid,
                "Grid columns and rows must be positive.",
                nameof(LayoutOptions.Grid)));
        }

        var metrics = LayoutCalculator.Calculate(layout);
        var pageWidthPx = Units.MmToPx(new Mm(metrics.PageWidthMm), dpi);
        var pageHeightPx = Units.MmToPx(new Mm(metrics.PageHeightMm), dpi);
        var cardWidthPx = Units.MmToPx(new Mm(metrics.CardWidthMm), dpi);
        var cardHeightPx = Units.MmToPx(new Mm(metrics.CardHeightMm), dpi);

        if (pageWidthPx <= 0 || pageHeightPx <= 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidPageSize,
                "Page size is too small at the current DPI.",
                nameof(LayoutOptions.Format)));
        }

        if (cardWidthPx <= 0 || cardHeightPx <= 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidCardSize,
                "Card size is too small at the current DPI.",
                nameof(LayoutOptions.CardWidth)));
        }

        if (metrics.PerPage < 1 || metrics.CardWidthMm <= 0 || metrics.CardHeightMm <= 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.LayoutDoesNotFit,
                "No cards fit on the configured page.",
                nameof(LayoutOptions)));
        }

        var bytesPerPage = (long)pageWidthPx * pageHeightPx * 4;
        if (bytesPerPage > EngineLimits.MaxPageBitmapBytes)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.ImageTooLarge,
                $"Estimated page bitmap {bytesPerPage:N0} bytes exceeds limit {EngineLimits.MaxPageBitmapBytes:N0} bytes.",
                nameof(LayoutOptions)));
        }

        if (layout.Bleed.Value > 0)
        {
            if (layout.Bleed.Value * 2 > layout.Gutter.Value)
            {
                warnings.Add(new ValidationError(
                    EngineErrorCode.InvalidBleed,
                    "Bleed is larger than half the gutter, so card bleeds may overlap.",
                    nameof(LayoutOptions.Bleed),
                    ValidationSeverity.Warning));
            }

            var minMargin = Math.Min(
                Math.Min(layout.MarginLeft.Value, layout.MarginRight.Value),
                Math.Min(layout.MarginTop.Value, layout.MarginBottom.Value));
            if (layout.Bleed.Value > minMargin)
            {
                warnings.Add(new ValidationError(
                    EngineErrorCode.InvalidBleed,
                    "Bleed exceeds the configured margins and may be clipped at the page edge.",
                    nameof(LayoutOptions.Bleed),
                    ValidationSeverity.Warning));
            }
        }
    }

    private void ValidateMeasurement(Mm measurement, string field, ICollection<ValidationError> errors, double? max = null)
    {
        var upper = max ?? EngineLimits.MaxMm;
        if (!measurement.IsFinite || measurement.Value < EngineLimits.MinMm || measurement.Value > upper)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidMeasurement,
                $"Measurement {field} must be between {EngineLimits.MinMm} and {upper} mm.",
                field));
        }
    }

    private ImageRef? NormalizeImage(ImageRef? image, string field, ICollection<ValidationError> errors)
    {
        if (image is null)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidImagePath,
                "Image reference is required.",
                field));
            return null;
        }

        if (!Enum.IsDefined(image.FitMode))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidMeasurement,
                "Fit mode is invalid.",
                $"{field}.{nameof(ImageRef.FitMode)}"));
        }

        if (!IsFinite(image.AnchorX) || image.AnchorX < 0 || image.AnchorX > 1)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidMeasurement,
                "AnchorX must be between 0 and 1.",
                $"{field}.{nameof(ImageRef.AnchorX)}"));
        }

        if (!IsFinite(image.AnchorY) || image.AnchorY < 0 || image.AnchorY > 1)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidMeasurement,
                "AnchorY must be between 0 and 1.",
                $"{field}.{nameof(ImageRef.AnchorY)}"));
        }

        if (!IsFinite(image.RotationDegrees))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidMeasurement,
                "Rotation must be a finite number.",
                $"{field}.{nameof(ImageRef.RotationDegrees)}"));
        }

        var normalizedPath = NormalizeFilePath(image.Path, errors, $"{field}.{nameof(ImageRef.Path)}");
        if (!string.IsNullOrWhiteSpace(normalizedPath) && !_fileSystem.FileExists(normalizedPath))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.ImageNotFound,
                $"Image file '{normalizedPath}' was not found.",
                $"{field}.{nameof(ImageRef.Path)}"));
        }

        return image with
        {
            Path = normalizedPath ?? image.Path,
        };
    }

    private string? NormalizeDirectory(string? path, ICollection<ValidationError> errors, string field)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidOutputDirectory,
                "Output directory is required.",
                field));
            return null;
        }

        var trimmed = path.Trim();
        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidOutputDirectory,
                $"Output directory '{trimmed}' is invalid: {ex.Message}",
                field));
            return trimmed;
        }
    }

    private string? NormalizeFilePath(string? path, ICollection<ValidationError> errors, string field)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidImagePath,
                "Image path is required.",
                field));
            return null;
        }

        var trimmed = path.Trim();
        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidImagePath,
                $"Image path '{trimmed}' is invalid: {ex.Message}",
                field));
            return trimmed;
        }
    }

    private string? NormalizePrefix(string? value, ICollection<ValidationError> errors, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidNamingPrefix,
                "Naming prefix is required.",
                field));
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > EngineLimits.MaxNamingPrefixLength)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidNamingPrefix,
                $"Naming prefix length must not exceed {EngineLimits.MaxNamingPrefixLength} characters.",
                field));
        }

        if (trimmed.Contains("..", StringComparison.Ordinal))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidNamingPrefix,
                "Naming prefix must not contain '..'.",
                field));
        }

        if (trimmed.IndexOfAny(DisallowedPrefixChars) >= 0)
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidNamingPrefix,
                "Naming prefix must not contain path separator characters.",
                field));
        }

        if (!IsPrefixWhitelisted(trimmed))
        {
            errors.Add(new ValidationError(
                EngineErrorCode.InvalidNamingPrefix,
                "Naming prefix may only contain letters, numbers, '-' or '_'.",
                field));
        }

        return trimmed;
    }

    private string NormalizeFormat(string? value, ICollection<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "png";
        }

        if (string.Equals(value, "png", StringComparison.OrdinalIgnoreCase))
        {
            return "png";
        }

        errors.Add(new ValidationError(
            EngineErrorCode.UnsupportedFormat,
            $"Format '{value}' is not supported. Only 'png' is allowed.",
            nameof(ExportRequest.Format)));
        return value;
    }

    private static ExportImageFormat ResolveFormat(string? value)
    {
        return string.Equals(value, "png", StringComparison.OrdinalIgnoreCase)
            ? ExportImageFormat.Png
            : ExportImageFormat.Png;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsWithinDirectory(string root, string fullPath)
    {
        var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : $"{root}{Path.DirectorySeparatorChar}";
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrefixWhitelisted(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
