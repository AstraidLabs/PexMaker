namespace PexMaker.API.Placeholder;

/// <summary>
/// Defines placeholder schema constants.
/// </summary>
public static class PlaceholderSchema
{
    /// <summary>
    /// Placeholder schema identifier.
    /// </summary>
    public const string Schema = "PexMaker.Placeholder/1";

    /// <summary>
    /// Placeholder schema version.
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// Placeholder file name.
    /// </summary>
    public const string FileName = "project.px.json";
}

/// <summary>
/// Represents placeholder metadata for a project.
/// </summary>
/// <param name="Schema">Placeholder schema identifier.</param>
/// <param name="Version">Placeholder schema version.</param>
/// <param name="ProjectId">Project identifier.</param>
/// <param name="DisplayName">Project display name.</param>
/// <param name="CreatedUtc">Created timestamp (UTC ISO 8601).</param>
/// <param name="UpdatedUtc">Updated timestamp (UTC ISO 8601).</param>
/// <param name="FrontFolder">Front source folder.</param>
/// <param name="BackFolder">Back source folder.</param>
/// <param name="ExportFolder">Export folder.</param>
/// <param name="TempFolder">Temporary folder.</param>
/// <param name="PairCountHint">Default pair count hint.</param>
/// <param name="DpiHint">Default DPI hint.</param>
/// <param name="Notes">Notes for the project.</param>
public sealed record ProjectPlaceholder(
    string Schema,
    int Version,
    string ProjectId,
    string DisplayName,
    string CreatedUtc,
    string UpdatedUtc,
    string FrontFolder,
    string BackFolder,
    string ExportFolder,
    string TempFolder,
    int PairCountHint,
    int DpiHint,
    string Notes
);

internal static class PlaceholderFactory
{
    internal static ProjectPlaceholder CreateNew(string projectId, string displayName, string nowUtcIso)
        => new(
            PlaceholderSchema.Schema,
            PlaceholderSchema.Version,
            projectId,
            displayName,
            nowUtcIso,
            nowUtcIso,
            "source/front",
            "source/back",
            "export",
            "tmp",
            12,
            300,
            string.Empty);
}
