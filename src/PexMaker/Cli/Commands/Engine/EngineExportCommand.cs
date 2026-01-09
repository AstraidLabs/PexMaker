using PexMaker.API;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using PexMaker.Engine.Application;
using PexMaker.Engine.Domain;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Engine;

internal sealed class EngineExportCommand : AsyncCommand<EngineExportCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public EngineExportCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandOption("--file <PATH>")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("--out <DIR>")]
        public string OutputDir { get; init; } = string.Empty;

        [CommandOption("--format <FMT>")]
        public string Format { get; init; } = "png";

        [CommandOption("--front")]
        public bool Front { get; init; }

        [CommandOption("--back")]
        public bool Back { get; init; }
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

            if (string.IsNullOrWhiteSpace(settings.OutputDir))
            {
                throw new CommandException("--out is required.");
            }

            if (!Directory.Exists(settings.OutputDir))
            {
                throw new CommandException($"Output directory does not exist: {settings.OutputDir}");
            }

            var api = _factory.Create(settings.Workspace);
            var json = await File.ReadAllTextAsync(settings.FilePath, cancellationToken).ConfigureAwait(false);
            var mappingMode = MappingModeParser.Parse(settings.Mapping);
            var includeFront = settings.Front || (!settings.Front && !settings.Back);
            var includeBack = settings.Back || (!settings.Front && !settings.Back);

            var progress = new Progress<EngineProgress>(progressUpdate =>
            {
                AnsiConsole.MarkupLine($"[grey]{progressUpdate.Stage} {progressUpdate.Current}/{progressUpdate.Total}[/]");
            });

            var request = new ExportRequest
            {
                OutputDirectory = settings.OutputDir,
                Format = settings.Format,
                IncludeFront = includeFront,
                IncludeBack = includeBack,
                Progress = progress,
            };

            var result = await api.ExportProjectJsonAsync(json, request, mappingMode, cancellationToken).ConfigureAwait(false);

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(result);
            }
            else
            {
                WriteExportSummary(result);
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

    private static void WriteExportSummary(ApiOperationResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Export: {status}");

        if (result.Data is ExportResult export)
        {
            var table = new Table();
            table.AddColumn("Produced files");

            if (export.ProducedFiles.Count == 0)
            {
                table.AddRow("(none)");
            }
            else
            {
                foreach (var file in export.ProducedFiles)
                {
                    table.AddRow(file);
                }
            }

            AnsiConsole.Write(table);
        }

        CliOutput.WriteIssues(result.Issues);
    }
}
