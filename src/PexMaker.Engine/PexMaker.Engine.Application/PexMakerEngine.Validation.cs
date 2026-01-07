using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    /// <summary>
    /// Validates a project and returns validation issues with severity.
    /// </summary>
    public Task<ProjectValidationResult> ValidateAsync(PexProject project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateProject(project, out _);
        return Task.FromResult(validation);
    }

    private ProjectValidationResult ValidateProject(PexProject project, out PexProject normalizedProject)
    {
        var validator = new EngineInputValidator(_fileSystem);
        return validator.ValidateProject(project, out normalizedProject);
    }

    private ProjectValidationResult ValidateExportRequest(ExportRequest request, out ExportRequest normalizedRequest)
    {
        var validator = new EngineInputValidator(_fileSystem);
        return validator.ValidateExportRequest(request, out normalizedRequest);
    }

    private ProjectValidationResult ValidateExportPaths(LayoutPlan layout, ExportRequest request)
    {
        var validator = new EngineInputValidator(_fileSystem);
        return validator.ValidateExportPaths(layout, request);
    }
}
