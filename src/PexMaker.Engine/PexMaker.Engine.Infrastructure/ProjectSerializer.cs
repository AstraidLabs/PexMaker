using System.Text.Json;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Infrastructure;

internal sealed class ProjectSerializer : IProjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task SerializeAsync(PexProject project, Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(destination);
        await JsonSerializer.SerializeAsync(destination, project, Options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PexProject> DeserializeAsync(Stream source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var project = await JsonSerializer.DeserializeAsync<PexProject>(source, Options, cancellationToken).ConfigureAwait(false);
        return project ?? throw new InvalidOperationException("Unable to deserialize project");
    }
}
