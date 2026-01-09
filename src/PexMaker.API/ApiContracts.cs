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

/// <summary>
/// Describes the result of importing image assets.
/// </summary>
/// <param name="Added">Number of files imported.</param>
/// <param name="Files">Imported file destinations.</param>
/// <param name="Issues">Issues captured during import.</param>
public sealed record ImportResult(
    int Added,
    IReadOnlyList<string> Files,
    IReadOnlyList<ApiIssue> Issues
);

/// <summary>
/// Options for building an engine project.json.
/// </summary>
/// <param name="PairCount">Optional override for pair count.</param>
/// <param name="Dpi">Optional override for output DPI.</param>
/// <param name="PresetId">Optional preset identifier.</param>
/// <param name="IncludeCutMarks">Whether to include cut marks.</param>
/// <param name="BleedMm">Bleed in millimeters.</param>
/// <param name="SafeAreaMm">Safe area inset in millimeters.</param>
public sealed record BuildProjectOptions(
    int? PairCount,
    int? Dpi,
    string? PresetId,
    bool IncludeCutMarks,
    double BleedMm,
    double SafeAreaMm
);

/// <summary>
/// Describes the result of building an engine project.json.
/// </summary>
/// <param name="Succeeded">Whether the build succeeded.</param>
/// <param name="ProjectJsonPath">Absolute path to the project.json.</param>
/// <param name="Issues">Issues captured during build.</param>
public sealed record BuildProjectResult(
    bool Succeeded,
    string ProjectJsonPath,
    IReadOnlyList<ApiIssue> Issues
);

/// <summary>
/// Provides information about a layout preset.
/// </summary>
/// <param name="Id">Preset identifier.</param>
/// <param name="Name">Preset display name.</param>
/// <param name="Description">Preset description.</param>
public sealed record LayoutPresetInfo(
    string Id,
    string Name,
    string? Description
);
