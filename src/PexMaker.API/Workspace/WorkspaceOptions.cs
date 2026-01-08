namespace PexMaker.API.Workspace;

/// <summary>
/// Defines workspace configuration options.
/// </summary>
/// <param name="WorkspaceRoot">Workspace root directory.</param>
public sealed record WorkspaceOptions(string WorkspaceRoot)
{
    /// <summary>
    /// Initializes default options using the current directory.
    /// </summary>
    public WorkspaceOptions()
        : this(Directory.GetCurrentDirectory())
    {
    }
}
