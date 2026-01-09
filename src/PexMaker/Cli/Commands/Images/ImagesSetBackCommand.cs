using PexMaker.API;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Images;

internal sealed class ImagesSetBackCommand : AsyncCommand<ImagesSetBackCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public ImagesSetBackCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<PROJECT_ID>")]
        public string ProjectId { get; init; } = string.Empty;

        [CommandOption("--file <PATH>")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("--move")]
        public bool Move { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.FilePath))
            {
                throw new CommandException("--file is required.");
            }

            var api = _factory.Create(settings.Workspace);
            var result = await api.SetBackImageAsync(
                settings.ProjectId,
                settings.FilePath,
                copy: !settings.Move,
                cancellationToken).ConfigureAwait(false);

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(result);
            }
            else
            {
                WriteImportSummary(result);
            }

            return 0;
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

    private static void WriteImportSummary(ImportResult result)
    {
        var table = new Table
        {
            Title = new TableTitle("Back image"),
        };

        table.AddColumn("Added");
        table.AddColumn("File");

        if (result.Files.Count == 0)
        {
            table.AddRow(result.Added.ToString(), "(none)");
        }
        else
        {
            table.AddRow(result.Added.ToString(), result.Files[0]);
        }

        AnsiConsole.Write(table);
        CliOutput.WriteIssues(result.Issues);
    }
}
