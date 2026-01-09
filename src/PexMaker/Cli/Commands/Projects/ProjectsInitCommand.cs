using PexMaker.API.Placeholder;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Projects;

internal sealed class ProjectsInitCommand : Command<ProjectsInitCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public ProjectsInitCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<PROJECT_ID>")]
        public string ProjectId { get; init; } = string.Empty;

        [CommandOption("--name <NAME>")]
        public string? Name { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ProjectId))
        {
            throw new CommandException("Project id is required.");
        }

        var api = _factory.Create(settings.Workspace);
        var paths = api.EnsureProject(settings.ProjectId, settings.Name);
        var placeholder = api.GetPlaceholder(settings.ProjectId);

        if (settings.JsonOutput)
        {
            CliOutput.WriteJson(new { Paths = paths, Placeholder = placeholder });
            return 0;
        }

        CliOutput.WriteKeyValueTable("Project paths", new[]
        {
            new KeyValuePair<string, string?>("Project ID", paths.ProjectId),
            new KeyValuePair<string, string?>("Project root", paths.ProjectRoot),
            new KeyValuePair<string, string?>("Placeholder", paths.PlaceholderPath),
            new KeyValuePair<string, string?>("Front folder", paths.FrontDir),
            new KeyValuePair<string, string?>("Back folder", paths.BackDir),
            new KeyValuePair<string, string?>("Export folder", paths.ExportDir),
            new KeyValuePair<string, string?>("Temp folder", paths.TempDir),
        });

        WritePlaceholderSummary(placeholder);
        return 0;
    }

    private static void WritePlaceholderSummary(ProjectPlaceholder placeholder)
    {
        CliOutput.WriteKeyValueTable("Placeholder", new[]
        {
            new KeyValuePair<string, string?>("Display name", placeholder.DisplayName),
            new KeyValuePair<string, string?>("Pair count", placeholder.PairCountHint.ToString()),
            new KeyValuePair<string, string?>("DPI", placeholder.DpiHint.ToString()),
            new KeyValuePair<string, string?>("Notes", placeholder.Notes),
            new KeyValuePair<string, string?>("Updated", placeholder.UpdatedUtc),
        });
    }
}
