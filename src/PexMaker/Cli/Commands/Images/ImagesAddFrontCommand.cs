using PexMaker.API;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Images;

internal sealed class ImagesAddFrontCommand : AsyncCommand<ImagesAddFrontCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public ImagesAddFrontCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<PROJECT_ID>")]
        public string ProjectId { get; init; } = string.Empty;

        [CommandOption("--from <PATH>")]
        public string[] From { get; init; } = Array.Empty<string>();

        [CommandOption("--recursive")]
        public bool Recursive { get; init; }

        [CommandOption("--move")]
        public bool Move { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            if (settings.From.Length == 0)
            {
                throw new CommandException("--from is required.");
            }

            var api = _factory.Create(settings.Workspace);
            var result = await api.AddFrontImagesAsync(
                settings.ProjectId,
                settings.From,
                settings.Recursive,
                copy: !settings.Move,
                context.CancellationToken).ConfigureAwait(false);

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(result);
            }
            else
            {
                WriteImportSummary("Front images", result);
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

    private static void WriteImportSummary(string title, ImportResult result)
    {
        var table = new Table
        {
            Title = new TableTitle(title),
        };

        table.AddColumn("Added");
        table.AddColumn("Files");

        if (result.Files.Count == 0)
        {
            table.AddRow(result.Added.ToString(), "(none)");
        }
        else
        {
            var first = true;
            foreach (var file in result.Files)
            {
                table.AddRow(first ? result.Added.ToString() : string.Empty, file);
                first = false;
            }
        }

        AnsiConsole.Write(table);
        CliOutput.WriteIssues(result.Issues);
    }
}
