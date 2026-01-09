using PexMaker.API;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Engine;

internal sealed class EngineValidateCommand : AsyncCommand<EngineValidateCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public EngineValidateCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandOption("--file <PATH>")]
        public string FilePath { get; init; } = string.Empty;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.FilePath))
            {
                throw new CommandException("--file is required.");
            }

            if (!File.Exists(settings.FilePath))
            {
                throw new CommandException($"File not found: {settings.FilePath}");
            }

            var api = _factory.Create(settings.Workspace);
            var json = await File.ReadAllTextAsync(settings.FilePath, cancellationToken).ConfigureAwait(false);
            var mappingMode = MappingModeParser.Parse(settings.Mapping);
            var result = await api.ValidateProjectJsonAsync(json, mappingMode, cancellationToken).ConfigureAwait(false);

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(result);
            }
            else
            {
                WriteValidationSummary(result);
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

    private static void WriteValidationSummary(ApiOperationResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Validation: {status}");
        CliOutput.WriteIssues(result.Issues);
    }
}
