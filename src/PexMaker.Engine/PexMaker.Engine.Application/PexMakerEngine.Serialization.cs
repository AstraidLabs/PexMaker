using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    public Task<SerializationResult> SaveProjectAsync(PexProject project, Stream destination, CancellationToken cancellationToken) => _projectSerializer.SaveAsync(project, destination, cancellationToken);

    public Task<DeserializationResult> LoadProjectAsync(Stream source, MappingMode mode, CancellationToken cancellationToken) => _projectSerializer.LoadAsync(source, mode, cancellationToken);
}
