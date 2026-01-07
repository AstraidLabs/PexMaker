using System;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Abstractions;

public interface IImageBuffer : IAsyncDisposable
{
    int Width { get; }
    int Height { get; }
}

public sealed record CardRenderOptions(
    int TargetWidth,
    int TargetHeight,
    FitMode FitMode,
    double AnchorX,
    double AnchorY,
    bool BorderEnabled,
    double BorderThickness,
    double CornerRadius)
{
    public CardRenderOptions WithSize(int width, int height) => this with { TargetWidth = width, TargetHeight = height };
}

public sealed record PagePlacement(IImageBuffer Image, int X, int Y, int Width, int Height);

public sealed record PageRenderRequest(int PageWidth, int PageHeight, IReadOnlyList<PagePlacement> Placements, bool IncludeCutMarks);

public enum ExportImageFormat
{
    Png,
    Jpeg,
    Webp,
}

public sealed class SheetExportRequest
{
    public required string OutputDirectory { get; init; }

    public required string NamingPrefixFront { get; init; }

    public required string NamingPrefixBack { get; init; }

    public ExportImageFormat Format { get; init; } = ExportImageFormat.Png;

    public required LayoutPlan Layout { get; init; }

    public required IReadOnlyList<ImageRef> Cards { get; init; }

    public required ImageRef BackImage { get; init; }

    public bool IncludeFront { get; init; } = true;

    public bool IncludeBack { get; init; } = true;

    public bool IncludeCutMarks { get; init; }

    public double CutMarkLengthPx { get; init; }

    public double CutMarkThicknessPx { get; init; }

    public double CutMarkOffsetPx { get; init; }

    public bool BorderEnabled { get; init; }

    public double BorderThicknessPx { get; init; }

    public double CornerRadiusPx { get; init; }

    public int CardWidthPx { get; init; }

    public int CardHeightPx { get; init; }

    public int BleedPx { get; init; }

    public bool EnableParallelism { get; init; }

    public int MaxDegreeOfParallelism { get; init; }

    public int MaxBufferedPages { get; init; }

    public int MaxBufferedCards { get; init; }

    public int MaxCacheItems { get; init; }

    public long MaxEstimatedWorkingSetBytes { get; init; }

    public IProgress<EngineProgress>? Progress { get; init; }
}

public sealed record SheetExportResult(IReadOnlyList<string> Files, ProjectValidationResult ValidationResult)
{
    public bool IsSuccess => ValidationResult.IsValid;
}

public interface IImageDecoder
{
    Task<IImageBuffer> DecodeAsync(Stream stream, CancellationToken cancellationToken);
}

public interface IImageProcessor
{
    Task<IImageBuffer> RenderCardAsync(IImageBuffer source, CardRenderOptions options, CancellationToken cancellationToken);
}

public interface IPageRenderer
{
    Task<IImageBuffer> ComposePageAsync(PageRenderRequest request, CancellationToken cancellationToken);
}

public interface ISheetExporter
{
    Task<SheetExportResult> ExportAsync(SheetExportRequest request, CancellationToken cancellationToken);
}
