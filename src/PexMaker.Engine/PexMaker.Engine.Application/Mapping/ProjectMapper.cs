using System.Linq;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Contracts;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application.Mapping;

internal static class ProjectMapper
{
    public static MappingResult<PexProjectDto> ToDto(PexProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var issues = new List<MappingIssue>();

        var frontImages = new List<ImageRefDto>();
        if (project.FrontImages is null)
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "Front images collection is missing.", nameof(project.FrontImages)));
        }
        else
        {
            foreach (var image in project.FrontImages)
            {
                var mapped = MapImageToDto(image, issues, nameof(project.FrontImages));
                if (mapped is not null)
                {
                    frontImages.Add(mapped);
                }
            }
        }

        var backImage = project.BackImage is not null
            ? MapImageToDto(project.BackImage, issues, nameof(project.BackImage))
            : null;

        if (backImage is null)
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "Back image is required.", nameof(project.BackImage)));
        }

        var dto = new PexProjectDto
        {
            SchemaVersion = ContractSchema.CurrentSchemaVersion,
            PairCount = project.PairCount,
            FrontImages = frontImages,
            BackImage = backImage,
            Layout = MapLayoutToDto(project.Layout),
            Dpi = project.Dpi.Value,
        };

        return new MappingResult<PexProjectDto>(dto, issues);
    }

    public static MappingResult<PexProject> ToDomain(PexProjectDto dto, MappingMode mode)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var issues = new List<MappingIssue>();
        ValidateSchemaVersion(dto.SchemaVersion, mode, issues);

        var pairCount = ResolvePairCount(dto.PairCount, mode, issues);
        var dpi = ResolveDpi(dto.Dpi, mode, issues);
        var layout = MapLayoutToDomain(dto.Layout, mode, issues);
        var frontImages = MapFrontImages(dto.FrontImages, mode, issues, pairCount);
        var backImage = MapSingleImage(dto.BackImage, mode, issues, nameof(dto.BackImage));

        var project = new PexProject
        {
            PairCount = pairCount,
            FrontImages = frontImages,
            BackImage = backImage,
            Layout = layout,
            Dpi = dpi,
        };

        var value = mode == MappingMode.Strict && HasErrors(issues) ? null : project;
        return new MappingResult<PexProject>(value, issues);
    }

    private static void ValidateSchemaVersion(int version, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (version <= 0)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, $"SchemaVersion missing; assuming {ContractSchema.CurrentSchemaVersion}.", nameof(PexProjectDto.SchemaVersion)));
            }
            else
            {
                issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "SchemaVersion is required.", nameof(PexProjectDto.SchemaVersion)));
            }

            return;
        }

        if (version > ContractSchema.CurrentSchemaVersion)
        {
            var severity = mode == MappingMode.Lenient ? MappingIssueSeverity.Warning : MappingIssueSeverity.Error;
            issues.Add(new MappingIssue(MappingIssueCode.UnsupportedSchemaVersion, $"SchemaVersion {version} exceeds supported version {ContractSchema.CurrentSchemaVersion}.", nameof(PexProjectDto.SchemaVersion), severity));
        }
    }

    private static int ResolvePairCount(int value, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (value >= 1 && value <= EngineLimits.MaxPairs)
        {
            return value;
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, $"PairCount {value} is invalid; defaulting to 1.", nameof(PexProject.PairCount)));
            return 1;
        }

        issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, $"PairCount {value} is invalid.", nameof(PexProject.PairCount)));
        return value;
    }

    private static Dpi ResolveDpi(int value, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (value >= EngineLimits.MinDpi && value <= EngineLimits.MaxDpi)
        {
            return Dpi.From(value);
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, $"DPI {value} is invalid; defaulting to {EngineDefaults.DefaultDpi}.", nameof(PexProject.Dpi)));
            return Dpi.From(EngineDefaults.DefaultDpi);
        }

        issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, $"DPI {value} is invalid.", nameof(PexProject.Dpi)));
        return Dpi.From(value);
    }

    private static LayoutOptions MapLayoutToDomain(LayoutOptionsDto? dto, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (dto is null)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Layout missing; applying defaults.", nameof(PexProject.Layout)));
                return new LayoutOptions();
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "Layout is required.", nameof(PexProject.Layout)));
            return new LayoutOptions();
        }

        var format = ParsePageFormat(dto.PageFormat, mode, issues);
        var orientation = dto.Landscape ? PageOrientation.Landscape : PageOrientation.Portrait;
        var margins = ResolveMargins(dto.Margins, mode, issues);
        var gutter = ResolveMeasurement(dto.GutterMm, EngineDefaults.DefaultGutter, nameof(LayoutOptions.Gutter), mode, issues);
        var cardSize = ResolveCardSize(dto.CardSizeMm, mode, issues);
        var cornerRadius = ResolveMeasurement(dto.CornerRadiusMm, EngineDefaults.DefaultCornerRadius, nameof(LayoutOptions.CornerRadius), mode, issues, allowZero: true);
        var borderThickness = ResolveMeasurement(dto.BorderThicknessMm, EngineDefaults.DefaultBorderThickness, nameof(LayoutOptions.BorderThickness), mode, issues, allowZero: true);
        var bleed = ResolveMeasurement(dto.BleedMm, Mm.Zero, nameof(LayoutOptions.Bleed), mode, issues, allowZero: true);
        var cutMarkLength = ResolveMeasurement(dto.CutMarkLengthMm, EngineDefaults.DefaultCutMarkLength, nameof(LayoutOptions.CutMarkLength), mode, issues, allowZero: true);
        var cutMarkThickness = ResolveMeasurement(dto.CutMarkThicknessMm, EngineDefaults.DefaultCutMarkThickness, nameof(LayoutOptions.CutMarkThickness), mode, issues, allowZero: true);
        var cutMarkOffset = ResolveMeasurement(dto.CutMarkOffsetMm, EngineDefaults.DefaultCutMarkOffset, nameof(LayoutOptions.CutMarkOffset), mode, issues, allowZero: true);
        var duplexMode = ParseDuplexMode(dto.DuplexMode, mode, issues);
        var alignment = ParseGridAlignment(dto.Alignment, mode, issues);
        var autoFitMode = ParseAutoFitMode(dto.AutoFitMode, mode, issues);
        var gridSpec = ResolveGridSpec(dto.GridColumns, dto.GridRows, mode, issues);

        return new LayoutOptions
        {
            Format = format,
            Orientation = orientation,
            MarginLeft = margins.Left,
            MarginTop = margins.Top,
            MarginRight = margins.Right,
            MarginBottom = margins.Bottom,
            Gutter = gutter,
            CardWidth = cardSize.Width,
            CardHeight = cardSize.Height,
            CornerRadius = cornerRadius,
            BorderEnabled = dto.DrawBorder,
            BorderThickness = borderThickness,
            MirrorBackside = dto.MirrorBackside,
            DuplexMode = duplexMode,
            Alignment = alignment,
            AutoFitMode = autoFitMode,
            Grid = gridSpec,
            PreferSquareCards = dto.PreferSquareCards,
            Bleed = bleed,
            CutMarks = dto.DrawCutMarks,
            CutMarkLength = cutMarkLength,
            CutMarkThickness = cutMarkThickness,
            CutMarkOffset = cutMarkOffset,
        };
    }

    private static (Mm Left, Mm Top, Mm Right, Mm Bottom) ResolveMargins(MarginDto? margins, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (margins is null)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Margins missing; applying defaults.", "Layout.Margins"));
                return (EngineDefaults.DefaultMargin, EngineDefaults.DefaultMargin, EngineDefaults.DefaultMargin, EngineDefaults.DefaultMargin);
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "Margins are required.", "Layout.Margins"));
            return (EngineDefaults.DefaultMargin, EngineDefaults.DefaultMargin, EngineDefaults.DefaultMargin, EngineDefaults.DefaultMargin);
        }

        var left = ResolveMeasurement(margins.LeftMm, EngineDefaults.DefaultMargin, "Layout.Margins.LeftMm", mode, issues);
        var top = ResolveMeasurement(margins.TopMm, EngineDefaults.DefaultMargin, "Layout.Margins.TopMm", mode, issues);
        var right = ResolveMeasurement(margins.RightMm, EngineDefaults.DefaultMargin, "Layout.Margins.RightMm", mode, issues);
        var bottom = ResolveMeasurement(margins.BottomMm, EngineDefaults.DefaultMargin, "Layout.Margins.BottomMm", mode, issues);

        return (left, top, right, bottom);
    }

    private static (Mm Width, Mm Height) ResolveCardSize(SizeDto? size, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (size is null)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "CardSize missing; applying defaults.", "Layout.CardSizeMm"));
                return (EngineDefaults.DefaultCardWidth, EngineDefaults.DefaultCardHeight);
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "CardSize is required.", "Layout.CardSizeMm"));
            return (EngineDefaults.DefaultCardWidth, EngineDefaults.DefaultCardHeight);
        }

        var width = ResolveMeasurement(size.WidthMm, EngineDefaults.DefaultCardWidth, "Layout.CardSizeMm.WidthMm", mode, issues, allowZero: false, mustBePositive: true);
        var height = ResolveMeasurement(size.HeightMm, EngineDefaults.DefaultCardHeight, "Layout.CardSizeMm.HeightMm", mode, issues, allowZero: false, mustBePositive: true);

        return (width, height);
    }

    private static PageFormat ParsePageFormat(string? value, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<PageFormat>(value, true, out var format))
        {
            return format;
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "PageFormat is missing or invalid; using default.", nameof(LayoutOptions.Format)));
            return EngineDefaults.DefaultPageFormat;
        }

        issues.Add(MappingIssue.Error(MappingIssueCode.UnknownEnum, "PageFormat is missing or invalid.", nameof(LayoutOptions.Format)));
        return EngineDefaults.DefaultPageFormat;
    }

    private static Mm ResolveMeasurement(double value, Mm fallback, string path, MappingMode mode, ICollection<MappingIssue> issues, bool allowZero = true, bool mustBePositive = false)
    {
        var isFinite = MathEx.IsFinite(value);
        var inRange = value >= EngineLimits.MinMm && value <= EngineLimits.MaxMm;
        var positive = !mustBePositive || value > 0;
        var valid = isFinite && inRange && (allowZero || value > 0) && positive;

        if (valid)
        {
            return Mm.From(value);
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, $"{path} is invalid; using default {fallback.Value}mm.", path));
            return fallback;
        }

        issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, $"{path} is invalid.", path));
        return fallback;
    }

    private static List<ImageRef> MapFrontImages(List<ImageRefDto>? dtos, MappingMode mode, ICollection<MappingIssue> issues, int pairCount)
    {
        if (dtos is null)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "FrontImages missing; using empty collection.", nameof(PexProject.FrontImages)));
                return new List<ImageRef>();
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "FrontImages are required.", nameof(PexProject.FrontImages)));
            return new List<ImageRef>();
        }

        var results = new List<ImageRef>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            var mapped = MapSingleImage(dtos[i], mode, issues, $"{nameof(PexProject.FrontImages)}[{i}]");
            if (mapped is not null)
            {
                results.Add(mapped);
            }
        }

        if (results.Count < pairCount)
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, $"FrontImages count {results.Count} is less than PairCount {pairCount}.", nameof(PexProject.FrontImages)));
        }

        return results;
    }

    private static ImageRef? MapSingleImage(ImageRefDto? dto, MappingMode mode, ICollection<MappingIssue> issues, string path)
    {
        if (dto is null)
        {
            var severity = mode == MappingMode.Lenient ? MappingIssueSeverity.Warning : MappingIssueSeverity.Error;
            issues.Add(new MappingIssue(MappingIssueCode.MissingValue, $"Image at {path} is missing.", path, severity));
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Path))
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "Image path is required.", CombinePath(path, nameof(ImageRefDto.Path))));
            return null;
        }

        var fitMode = ParseFitMode(dto.FitMode, mode, CombinePath(path, nameof(ImageRefDto.FitMode)), issues);
        var anchorX = NormalizeAnchor(dto.CropX, mode, CombinePath(path, nameof(ImageRefDto.CropX)), issues);
        var anchorY = NormalizeAnchor(dto.CropY, mode, CombinePath(path, nameof(ImageRefDto.CropY)), issues);
        var rotation = NormalizeRotation(dto.RotationDeg, mode, CombinePath(path, nameof(ImageRefDto.RotationDeg)), issues);

        if (fitMode is null)
        {
            return null;
        }

        return new ImageRef
        {
            Path = dto.Path,
            FitMode = fitMode.Value,
            AnchorX = anchorX,
            AnchorY = anchorY,
            RotationDegrees = rotation,
        };
    }

    private static ImageRefDto? MapImageToDto(ImageRef? image, ICollection<MappingIssue> issues, string path)
    {
        if (image is null)
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, $"Image at {path} is missing.", path));
            return null;
        }

        return new ImageRefDto
        {
            Path = image.Path,
            FitMode = image.FitMode.ToString(),
            CropX = (float)image.AnchorX,
            CropY = (float)image.AnchorY,
            RotationDeg = (float)image.RotationDegrees,
        };
    }

    private static FitMode? ParseFitMode(string? value, MappingMode mode, string path, ICollection<MappingIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<FitMode>(value, true, out var fitMode))
        {
            return fitMode;
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "FitMode is missing or invalid; using default.", path));
            return EngineDefaults.DefaultFitMode;
        }

        issues.Add(MappingIssue.Error(MappingIssueCode.UnknownEnum, "FitMode is missing or invalid.", path));
        return null;
    }

    private static double NormalizeAnchor(double value, MappingMode mode, string path, ICollection<MappingIssue> issues)
    {
        if (!MathEx.IsFinite(value))
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Anchor value is not finite; using 0.5.", path));
                return 0.5;
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, "Anchor value is not finite.", path));
            return value;
        }

        if (value < 0 || value > 1)
        {
            if (mode == MappingMode.Lenient)
            {
                var clamped = MathEx.Clamp01(value);
                issues.Add(MappingIssue.Warning(MappingIssueCode.ClampedValue, $"Anchor value {value} clamped to {clamped}.", path));
                return clamped;
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, "Anchor value must be between 0 and 1.", path));
        }

        return MathEx.Clamp01(value);
    }

    private static double NormalizeRotation(float? value, MappingMode mode, string path, ICollection<MappingIssue> issues)
    {
        if (value is null)
        {
            return 0;
        }

        if (!float.IsFinite(value.Value))
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Rotation is not finite; using 0.", path));
                return 0;
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, "Rotation is not finite.", path));
            return value.Value;
        }

        return value.Value;
    }

    private static LayoutOptionsDto MapLayoutToDto(LayoutOptions layout)
    {
        return new LayoutOptionsDto
        {
            PageFormat = layout.Format.ToString(),
            Landscape = layout.Orientation == PageOrientation.Landscape,
            Margins = new MarginDto
            {
                LeftMm = layout.MarginLeft.Value,
                TopMm = layout.MarginTop.Value,
                RightMm = layout.MarginRight.Value,
                BottomMm = layout.MarginBottom.Value,
            },
            GutterMm = layout.Gutter.Value,
            CardSizeMm = new SizeDto
            {
                WidthMm = layout.CardWidth.Value,
                HeightMm = layout.CardHeight.Value,
            },
            CornerRadiusMm = layout.CornerRadius.Value,
            MirrorBackside = layout.MirrorBackside,
            DuplexMode = layout.DuplexMode.ToString(),
            Alignment = layout.Alignment.ToString(),
            AutoFitMode = layout.AutoFitMode.ToString(),
            GridColumns = layout.Grid?.Columns,
            GridRows = layout.Grid?.Rows,
            PreferSquareCards = layout.PreferSquareCards,
            DrawBorder = layout.BorderEnabled,
            BorderThicknessMm = layout.BorderThickness.Value,
            BleedMm = layout.Bleed.Value,
            DrawCutMarks = layout.CutMarks,
            CutMarkLengthMm = layout.CutMarkLength.Value,
            CutMarkThicknessMm = layout.CutMarkThickness.Value,
            CutMarkOffsetMm = layout.CutMarkOffset.Value,
        };
    }

    private static DuplexMode ParseDuplexMode(string? value, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<DuplexMode>(value, true, out var duplexMode))
        {
            return duplexMode;
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "DuplexMode is missing or invalid; using default.", nameof(LayoutOptions.DuplexMode)));
        }
        else if (!string.IsNullOrWhiteSpace(value))
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.UnknownEnum, "DuplexMode is invalid.", nameof(LayoutOptions.DuplexMode)));
        }

        return EngineDefaults.DefaultDuplexMode;
    }

    private static GridAlignment ParseGridAlignment(string? value, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<GridAlignment>(value, true, out var alignment))
        {
            return alignment;
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Alignment is missing or invalid; using default.", nameof(LayoutOptions.Alignment)));
        }
        else if (!string.IsNullOrWhiteSpace(value))
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.UnknownEnum, "Alignment is invalid.", nameof(LayoutOptions.Alignment)));
        }

        return EngineDefaults.DefaultGridAlignment;
    }

    private static AutoFitMode ParseAutoFitMode(string? value, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<AutoFitMode>(value, true, out var autoFitMode))
        {
            return autoFitMode;
        }

        if (mode == MappingMode.Lenient)
        {
            issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "AutoFitMode is missing or invalid; using default.", nameof(LayoutOptions.AutoFitMode)));
        }
        else if (!string.IsNullOrWhiteSpace(value))
        {
            issues.Add(MappingIssue.Error(MappingIssueCode.UnknownEnum, "AutoFitMode is invalid.", nameof(LayoutOptions.AutoFitMode)));
        }

        return EngineDefaults.DefaultAutoFitMode;
    }

    private static GridSpec? ResolveGridSpec(int? columns, int? rows, MappingMode mode, ICollection<MappingIssue> issues)
    {
        if (columns is null && rows is null)
        {
            return null;
        }

        if (columns is null || rows is null)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Grid specification is incomplete; ignoring.", nameof(LayoutOptions.Grid)));
                return null;
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.MissingValue, "GridColumns and GridRows must both be provided.", nameof(LayoutOptions.Grid)));
            return null;
        }

        if (columns <= 0 || rows <= 0)
        {
            if (mode == MappingMode.Lenient)
            {
                issues.Add(MappingIssue.Warning(MappingIssueCode.DefaultApplied, "Grid specification is invalid; ignoring.", nameof(LayoutOptions.Grid)));
                return null;
            }

            issues.Add(MappingIssue.Error(MappingIssueCode.InvalidValue, "GridColumns and GridRows must be positive.", nameof(LayoutOptions.Grid)));
            return null;
        }

        return new GridSpec(columns.Value, rows.Value);
    }

    private static bool HasErrors(IEnumerable<MappingIssue> issues) => issues.Any(issue => issue.Severity == MappingIssueSeverity.Error);

    private static string CombinePath(string prefix, string property) => string.IsNullOrWhiteSpace(prefix) ? property : $"{prefix}.{property}";
}
