using PexMaker.API;
using PexMaker.API.Workspace;

namespace PexMaker.Cli.Infrastructure;

internal interface IPexMakerApiFactory
{
    IPexMakerApi Create(string? workspaceRoot);
}

internal sealed class PexMakerApiFactory : IPexMakerApiFactory
{
    public IPexMakerApi Create(string? workspaceRoot)
    {
        var root = string.IsNullOrWhiteSpace(workspaceRoot)
            ? Directory.GetCurrentDirectory()
            : workspaceRoot;
        var options = new WorkspaceOptions(root);
        return new PexMakerApi(options);
    }
}
