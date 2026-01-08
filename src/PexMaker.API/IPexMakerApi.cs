using PexMaker.API.Placeholder;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Application;

namespace PexMaker.API;

/// <summary>
/// Defines the PexMaker public API surface.
/// </summary>
public interface IPexMakerApi
{
    /// <summary>
    /// Gets workspace root paths.
    /// </summary>
    WorkspacePaths GetWorkspacePaths();

    /// <summary>
    /// Lists available project identifiers.
    /// </summary>
    IReadOnlyList<string> ListProjects();

    /// <summary>
    /// Ensures a project exists and returns its paths.
    /// </summary>
    ProjectPaths EnsureProject(string projectId, string? displayName = null);

    /// <summary>
    /// Loads the placeholder metadata for a project.
    /// </summary>
    ProjectPlaceholder GetPlaceholder(string projectId);

    /// <summary>
    /// Saves placeholder metadata for a project.
    /// </summary>
    ProjectPlaceholder SavePlaceholder(string projectId, ProjectPlaceholder placeholder);

    /// <summary>
    /// Validates project JSON input.
    /// </summary>
    Task<ApiOperationResult> ValidateProjectJsonAsync(
        string projectJson,
        MappingMode mappingMode,
        CancellationToken ct);

    /// <summary>
    /// Previews a layout from project JSON.
    /// </summary>
    Task<ApiOperationResult> PreviewProjectJsonAsync(
        string projectJson,
        string? presetId,
        MappingMode mappingMode,
        CancellationToken ct);

    /// <summary>
    /// Exports a project from project JSON.
    /// </summary>
    Task<ApiOperationResult> ExportProjectJsonAsync(
        string projectJson,
        ExportRequest request,
        MappingMode mappingMode,
        CancellationToken ct);
}
