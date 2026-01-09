using PexMaker.API.Placeholder;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Projects;

internal sealed class ProjectsShowCommand : Command<ProjectsShowCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public ProjectsShowCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<PROJECT_ID>")]
        public string ProjectId { get; init; } = string.Empty;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ProjectId))
        {
            throw new CommandException("Project id is required.");
        }

        var api = _factory.Create(settings.Workspace);
        var placeholder = api.GetPlaceholder(settings.ProjectId);

        if (settings.JsonOutput)
        {
            CliOutput.WriteJson(placeholder);
            return 0;
        }

        WritePlaceholderSummary(placeholder);
        return 0;
    }

    private static void WritePlaceholderSummary(ProjectPlaceholder placeholder)
    {
        CliOutput.WriteKeyValueTable("Placeholder", new[]
        {
            new KeyValuePair<string, string?>("Project ID", placeholder.ProjectId),
            new KeyValuePair<string, string?>("Display name", placeholder.DisplayName),
            new KeyValuePair<string, string?>("Pair count", placeholder.PairCountHint.ToString()),
            new KeyValuePair<string, string?>("DPI", placeholder.DpiHint.ToString()),
            new KeyValuePair<string, string?>("Notes", placeholder.Notes),
            new KeyValuePair<string, string?>("Updated", placeholder.UpdatedUtc),
        });
    }
}
