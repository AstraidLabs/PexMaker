using System.Text;
using PexMaker.API.Placeholder;
using PexMaker.API.Workspace;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Application;
using PexMaker.Engine.Domain;
using PexMaker.Engine.Infrastructure;

namespace PexMaker.API;

/// <summary>
/// Provides a simple API for working with PexMaker workspaces and engine operations.
/// </summary>
public sealed class PexMakerApi : IPexMakerApi
{
    private readonly WorkspaceManager _workspace;

    /// <summary>
    /// Creates a new API instance.
    /// </summary>
    /// <param name="options">Workspace options.</param>
    public PexMakerApi(WorkspaceOptions? options = null)
    {
        _workspace = new WorkspaceManager(options ?? new WorkspaceOptions());
    }

    /// <inheritdoc />
    public WorkspacePaths GetWorkspacePaths() => _workspace.GetWorkspacePaths();

    /// <inheritdoc />
    public IReadOnlyList<string> ListProjects() => _workspace.ListProjects();

    /// <inheritdoc />
    public ProjectPaths EnsureProject(string projectId, string? displayName = null)
        => _workspace.EnsureProject(projectId, displayName);

    /// <inheritdoc />
    public ProjectPlaceholder GetPlaceholder(string projectId)
        => _workspace.GetPlaceholder(projectId);

    /// <inheritdoc />
    public ProjectPlaceholder SavePlaceholder(string projectId, ProjectPlaceholder placeholder)
        => _workspace.SavePlaceholder(projectId, placeholder);

    /// <summary>
    /// Validates project JSON with lenient mapping.
    /// </summary>
    public Task<ApiOperationResult> ValidateProjectJsonAsync(string projectJson, CancellationToken ct)
        => ValidateProjectJsonAsync(projectJson, MappingMode.Lenient, ct);

    /// <summary>
    /// Previews project JSON with lenient mapping.
    /// </summary>
    public Task<ApiOperationResult> PreviewProjectJsonAsync(string projectJson, string? presetId, CancellationToken ct)
        => PreviewProjectJsonAsync(projectJson, presetId, MappingMode.Lenient, ct);

    /// <summary>
    /// Exports project JSON with lenient mapping.
    /// </summary>
    public Task<ApiOperationResult> ExportProjectJsonAsync(string projectJson, ExportRequest request, CancellationToken ct)
        => ExportProjectJsonAsync(projectJson, request, MappingMode.Lenient, ct);

    /// <inheritdoc />
    public async Task<ApiOperationResult> ValidateProjectJsonAsync(string projectJson, MappingMode mappingMode, CancellationToken ct)
    {
        var loadResult = await LoadProjectAsync(projectJson, mappingMode, ct).ConfigureAwait(false);
        if (!loadResult.Succeeded)
        {
            return new ApiOperationResult(false, null, loadResult.Issues);
        }

        var engine = CreateEngine();
        var validationResult = await engine.ValidateAsync(loadResult.Project!, ct).ConfigureAwait(false);
        var issues = loadResult.Issues.Concat(MapValidationResult(validationResult)).ToArray();
        return new ApiOperationResult(validationResult.IsValid, validationResult, issues);
    }

    /// <inheritdoc />
    public async Task<ApiOperationResult> PreviewProjectJsonAsync(string projectJson, string? presetId, MappingMode mappingMode, CancellationToken ct)
    {
        var loadResult = await LoadProjectAsync(projectJson, mappingMode, ct).ConfigureAwait(false);
        if (!loadResult.Succeeded)
        {
            return new ApiOperationResult(false, null, loadResult.Issues);
        }

        var engine = CreateEngine();
        var preview = engine.PreviewLayout(loadResult.Project!, presetId);
        var issues = loadResult.Issues.Concat(MapValidationResult(loadResult.ValidationResult)).ToArray();
        return new ApiOperationResult(true, preview, issues);
    }

    /// <inheritdoc />
    public async Task<ApiOperationResult> ExportProjectJsonAsync(string projectJson, ExportRequest request, MappingMode mappingMode, CancellationToken ct)
    {
        var loadResult = await LoadProjectAsync(projectJson, mappingMode, ct).ConfigureAwait(false);
        if (!loadResult.Succeeded)
        {
            return new ApiOperationResult(false, null, loadResult.Issues);
        }

        var engine = CreateEngine();
        var exportResult = await engine.ExportAsync(loadResult.Project!, request, ct).ConfigureAwait(false);
        var issues = loadResult.Issues.Concat(MapValidationResult(exportResult.ValidationResult)).ToArray();
        return new ApiOperationResult(exportResult.Succeeded, exportResult, issues);
    }

    internal static PexMakerEngine CreateEngine() => PexMakerEngineFactory.CreateDefault();

    private static async Task<LoadProjectResult> LoadProjectAsync(string projectJson, MappingMode mappingMode, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(projectJson);

        var engine = CreateEngine();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(projectJson));
        var result = await engine.LoadProjectAsync(stream, mappingMode, ct).ConfigureAwait(false);

        if (result.Value?.Project is null)
        {
            return LoadProjectResult.Failure(new ApiIssue(
                "ProjectJsonInvalid",
                ApiSeverity.Error,
                "Project JSON could not be loaded."));
        }

        var issues = MapSerializationIssues(result.Value.Issues).ToList();
        return LoadProjectResult.Success(result.Value.Project, result.ValidationResult, issues);
    }

    private static IEnumerable<ApiIssue> MapSerializationIssues(IEnumerable<SerializationIssue> issues)
    {
        foreach (var issue in issues)
        {
            yield return new ApiIssue(
                issue.Code,
                issue.Severity == IssueSeverity.Warning ? ApiSeverity.Warning : ApiSeverity.Error,
                issue.Message,
                issue.Path);
        }
    }

    private static IEnumerable<ApiIssue> MapValidationResult(ProjectValidationResult validationResult)
        => validationResult.Issues.Select(MapValidationIssue);

    private static ApiIssue MapValidationIssue(ValidationError issue)
        => new(
            issue.Code.ToString(),
            issue.Severity == ValidationSeverity.Warning ? ApiSeverity.Warning : ApiSeverity.Error,
            issue.Message,
            issue.Field);

    private sealed record LoadProjectResult(
        PexProject? Project,
        ProjectValidationResult ValidationResult,
        IReadOnlyList<ApiIssue> Issues,
        bool Succeeded)
    {
        internal static LoadProjectResult Failure(ApiIssue issue)
            => new(null, ProjectValidationResult.Failure(new[] { new ValidationError(EngineErrorCode.ProjectLoadFailed, issue.Message, issue.Path) }), new[] { issue }, false);

        internal static LoadProjectResult Success(PexProject project, ProjectValidationResult validationResult, IReadOnlyList<ApiIssue> issues)
            => new(project, validationResult, issues, true);
    }
}
