using PexMaker.API;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Projects;

internal sealed class ProjectBuildCommand : AsyncCommand<ProjectBuildCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public ProjectBuildCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<PROJECT_ID>")]
        public string ProjectId { get; init; } = string.Empty;

        [CommandOption("--pairs <N>")]
        public int? PairCount { get; init; }

        [CommandOption("--dpi <N>")]
        public int? Dpi { get; init; }

        [CommandOption("--preset <ID>")]
        public string? PresetId { get; init; }

        [CommandOption("--bleed <MM>")]
        public double BleedMm { get; init; }

        [CommandOption("--safe <MM>")]
        public double SafeAreaMm { get; init; }

        [CommandOption("--cut-marks")]
        public bool IncludeCutMarks { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var api = _factory.Create(settings.Workspace);
            var options = new BuildProjectOptions(
                settings.PairCount,
                settings.Dpi,
                settings.PresetId,
                settings.IncludeCutMarks,
                settings.BleedMm,
                settings.SafeAreaMm);

            var result = await api.BuildEngineProjectAsync(settings.ProjectId, options, context.CancellationToken)
                .ConfigureAwait(false);

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(result);
            }
            else
            {
                WriteBuildSummary(result);
            }

            return result.Succeeded ? 0 : 2;
        }
        catch (CommandException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CommandException(ex.Message);
        }
    }

    private static void WriteBuildSummary(BuildProjectResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Build: {status}");

        var table = new Table();
        table.AddColumn("Project JSON");
        table.AddRow(result.ProjectJsonPath);
        AnsiConsole.Write(table);

        CliOutput.WriteIssues(result.Issues);
    }
}
