using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly ISheetExporter _sheetExporter;
    private readonly IRandomProvider _randomProvider;
    private readonly IProjectSerializer _projectSerializer;

    public PexMakerEngine(
        IFileSystem fileSystem,
        ISheetExporter sheetExporter,
        IRandomProvider randomProvider,
        IProjectSerializer projectSerializer)
    {
        _fileSystem = Guard.NotNull(fileSystem, nameof(fileSystem));
        _sheetExporter = Guard.NotNull(sheetExporter, nameof(sheetExporter));
        _randomProvider = Guard.NotNull(randomProvider, nameof(randomProvider));
        _projectSerializer = Guard.NotNull(projectSerializer, nameof(projectSerializer));
    }
}
