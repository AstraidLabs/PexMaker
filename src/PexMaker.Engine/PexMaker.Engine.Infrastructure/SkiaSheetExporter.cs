using PexMaker.Engine.Abstractions;

namespace PexMaker.Engine.Infrastructure;

internal sealed partial class SkiaSheetExporter : ISheetExporter
{
    private readonly IFileSystem _fileSystem;
    private readonly SkiaPageRasterizer _pageRasterizer;

    public SkiaSheetExporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _pageRasterizer = new SkiaPageRasterizer(fileSystem);
    }

    public Task<SheetExportResult> ExportAsync(SheetExportRequest request, CancellationToken cancellationToken)
        => ExportInternalAsync(request, cancellationToken);
}
