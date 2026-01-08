using System.Collections.Generic;

namespace PexMaker.API;

/// <summary>
/// Represents severity of an API issue.
/// </summary>
public enum ApiSeverity
{
    /// <summary>
    /// Informational note.
    /// </summary>
    Info,

    /// <summary>
    /// Warning condition.
    /// </summary>
    Warning,

    /// <summary>
    /// Error condition.
    /// </summary>
    Error,
}

/// <summary>
/// Describes a validation or processing issue.
/// </summary>
/// <param name="Code">Stable issue code.</param>
/// <param name="Severity">Issue severity.</param>
/// <param name="Message">Human-readable message.</param>
/// <param name="Path">Optional path or field reference.</param>
/// <param name="Recommendation">Optional remediation guidance.</param>
public sealed record ApiIssue(
    string Code,
    ApiSeverity Severity,
    string Message,
    string? Path = null,
    string? Recommendation = null
);

/// <summary>
/// Contains root paths for a workspace.
/// </summary>
/// <param name="Root">Workspace root path.</param>
/// <param name="ProjectsRoot">Projects root path.</param>
public sealed record WorkspacePaths(
    string Root,
    string ProjectsRoot
);

/// <summary>
/// Paths for a specific project.
/// </summary>
/// <param name="ProjectId">Project identifier.</param>
/// <param name="ProjectRoot">Project root path.</param>
/// <param name="PlaceholderPath">Project placeholder path.</param>
/// <param name="FrontDir">Front source directory.</param>
/// <param name="BackDir">Back source directory.</param>
/// <param name="ExportDir">Export directory.</param>
/// <param name="TempDir">Temporary directory.</param>
public sealed record ProjectPaths(
    string ProjectId,
    string ProjectRoot,
    string PlaceholderPath,
    string FrontDir,
    string BackDir,
    string ExportDir,
    string TempDir
);

/// <summary>
/// Represents a standardized API operation result.
/// </summary>
/// <param name="Succeeded">Whether the operation succeeded.</param>
/// <param name="Data">Optional payload.</param>
/// <param name="Issues">Issues captured during the operation.</param>
public sealed record ApiOperationResult(
    bool Succeeded,
    object? Data,
    IReadOnlyList<ApiIssue> Issues
);
