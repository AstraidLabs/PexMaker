using PexMaker.Engine.Abstractions;

namespace PexMaker.Engine.Infrastructure;

internal sealed partial class SkiaSheetExporter : ISheetExporter
{
    private readonly IFileSystem _fileSystem;

    public SkiaSheetExporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public Task<SheetExportResult> ExportAsync(SheetExportRequest request, CancellationToken cancellationToken)
        => ExportInternalAsync(request, cancellationToken);
}
