using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Projects;

internal sealed class ProjectsListCommand : Command<BaseSettings>
{
    private readonly IPexMakerApiFactory _factory;

    public ProjectsListCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        var api = _factory.Create(settings.Workspace);
        var projects = api.ListProjects();
        var paths = api.GetWorkspacePaths();

        if (settings.JsonOutput)
        {
            CliOutput.WriteJson(projects);
            return 0;
        }

        AnsiConsole.MarkupLine($"Workspace projects root: [blue]{paths.ProjectsRoot.EscapeMarkup()}[/]");

        var table = new Table();
        table.AddColumn("Project ID");

        if (projects.Count == 0)
        {
            table.AddRow("(none)");
        }
        else
        {
            foreach (var projectId in projects)
            {
                table.AddRow(projectId);
            }
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
