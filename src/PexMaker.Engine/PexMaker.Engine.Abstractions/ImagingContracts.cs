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

public enum SheetSide
{
    Front,
    Back,
}

public enum ExportImageFormat
{
    Png,
    Jpeg,
    Webp,
}

public sealed record SheetExportEntry(string FileName, IImageBuffer Image, SheetSide Side);

public sealed record SheetExportRequest(string Directory, ExportImageFormat Format, IReadOnlyList<SheetExportEntry> Sheets);

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
    Task<IReadOnlyList<string>> ExportAsync(SheetExportRequest request, CancellationToken cancellationToken);
}
