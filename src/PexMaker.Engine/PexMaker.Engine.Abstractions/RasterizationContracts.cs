using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Abstractions;

public sealed class PageRasterizerRequest
{
    public required LayoutPlan Layout { get; init; }

    public required LayoutPage Page { get; init; }

    public required IReadOnlyList<ImageRef> Cards { get; init; }

    public required ImageRef BackImage { get; init; }

    public bool IncludeCutMarks { get; init; }

    public bool CutMarksPerCard { get; init; }

    public double CutMarkLengthPx { get; init; }

    public double CutMarkThicknessPx { get; init; }

    public double CutMarkOffsetPx { get; init; }

    public bool BorderEnabled { get; init; }

    public double BorderThicknessPx { get; init; }

    public double CornerRadiusPx { get; init; }

    public int CardWidthPx { get; init; }

    public int CardHeightPx { get; init; }

    public int BleedPx { get; init; }

    public int SafeAreaPx { get; init; }

    public bool ShowSafeAreaOverlay { get; init; }

    public int SafeAreaOverlayThicknessPx { get; init; }

    public bool IncludeRegistrationMarks { get; init; }

    public RegistrationMarkPlacement RegistrationMarkPlacement { get; init; } = RegistrationMarkPlacement.CornersOutsideGrid;

    public double RegistrationMarkSizePx { get; init; }

    public double RegistrationMarkThicknessPx { get; init; }

    public double RegistrationMarkOffsetPx { get; init; }

    public bool EnableParallelism { get; init; }

    public int MaxDegreeOfParallelism { get; init; }

    public int MaxCacheItems { get; init; }
}

public sealed record PageRasterizerResult(int PageNumber, SheetSide Side, byte[] PngBytes);

public interface IPageRasterizer
{
    Task<PageRasterizerResult> RenderPageAsync(PageRasterizerRequest request, CancellationToken cancellationToken);
}
