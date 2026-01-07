using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    /// <summary>
    /// Saves a project after validating inputs.
    /// </summary>
    public async Task<EngineOperationResult<SerializationResult>> SaveProjectAsync(PexProject project, Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateProject(project, out var normalizedProject);
        if (!validation.IsValid)
        {
            return new EngineOperationResult<SerializationResult>(null, validation);
        }

        var result = await _projectSerializer.SaveAsync(normalizedProject, destination, cancellationToken).ConfigureAwait(false);
        return new EngineOperationResult<SerializationResult>(result, validation);
    }

    /// <summary>
    /// Loads a project and validates the resulting project data.
    /// </summary>
    public async Task<EngineOperationResult<DeserializationResult>> LoadProjectAsync(Stream source, MappingMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _projectSerializer.LoadAsync(source, mode, cancellationToken).ConfigureAwait(false);
        if (result.Project is null)
        {
            var error = new ValidationError(
                EngineErrorCode.ProjectLoadFailed,
                "Project data could not be loaded.",
                nameof(DeserializationResult.Project));
            var validation = ProjectValidationResult.Failure(new[] { error });
            return new EngineOperationResult<DeserializationResult>(result, validation);
        }

        var validationResult = ValidateProject(result.Project, out var normalizedProject);
        var normalizedResult = result with { Project = normalizedProject };
        return new EngineOperationResult<DeserializationResult>(normalizedResult, validationResult);
    }
}
