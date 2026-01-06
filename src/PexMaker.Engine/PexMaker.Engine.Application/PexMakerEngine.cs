using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly IImageDecoder _imageDecoder;
    private readonly IImageProcessor _imageProcessor;
    private readonly IPageRenderer _pageRenderer;
    private readonly ISheetExporter _sheetExporter;
    private readonly IRandomProvider _randomProvider;
    private readonly IProjectSerializer _projectSerializer;

    public PexMakerEngine(
        IFileSystem fileSystem,
        IImageDecoder imageDecoder,
        IImageProcessor imageProcessor,
        IPageRenderer pageRenderer,
        ISheetExporter sheetExporter,
        IRandomProvider randomProvider,
        IProjectSerializer projectSerializer)
    {
        _fileSystem = Guard.NotNull(fileSystem, nameof(fileSystem));
        _imageDecoder = Guard.NotNull(imageDecoder, nameof(imageDecoder));
        _imageProcessor = Guard.NotNull(imageProcessor, nameof(imageProcessor));
        _pageRenderer = Guard.NotNull(pageRenderer, nameof(pageRenderer));
        _sheetExporter = Guard.NotNull(sheetExporter, nameof(sheetExporter));
        _randomProvider = Guard.NotNull(randomProvider, nameof(randomProvider));
        _projectSerializer = Guard.NotNull(projectSerializer, nameof(projectSerializer));
    }
}
