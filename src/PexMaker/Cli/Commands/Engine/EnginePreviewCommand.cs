using PexMaker.API;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using PexMaker.Engine.Application;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Engine;

internal sealed class EnginePreviewCommand : AsyncCommand<EnginePreviewCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public EnginePreviewCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandOption("--file <PATH>")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("--preset <ID>")]
        public string? PresetId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
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
            var json = await File.ReadAllTextAsync(settings.FilePath, context.CancellationToken).ConfigureAwait(false);
            var mappingMode = MappingModeParser.Parse(settings.Mapping);
            var result = await api.PreviewProjectJsonAsync(json, settings.PresetId, mappingMode, context.CancellationToken)
                .ConfigureAwait(false);

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(result);
            }
            else
            {
                WritePreviewSummary(result);
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

    private static void WritePreviewSummary(ApiOperationResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Preview: {status}");

        if (result.Data is LayoutPreview preview)
        {
            CliOutput.WriteKeyValueTable("Layout preview", new[]
            {
                new KeyValuePair<string, string?>("Preset", preview.PresetId ?? "(default)"),
                new KeyValuePair<string, string?>("Columns", preview.Columns.ToString()),
                new KeyValuePair<string, string?>("Rows", preview.Rows.ToString()),
                new KeyValuePair<string, string?>("Per page", preview.PerPage.ToString()),
                new KeyValuePair<string, string?>("Pages", preview.Pages.ToString()),
                new KeyValuePair<string, string?>("Card (mm)", $"{preview.CardWidthMm:0.##} x {preview.CardHeightMm:0.##}"),
                new KeyValuePair<string, string?>("Page (mm)", $"{preview.PageWidthMm:0.##} x {preview.PageHeightMm:0.##}"),
                new KeyValuePair<string, string?>("Card (px)", $"{preview.CardWidthPx} x {preview.CardHeightPx}"),
                new KeyValuePair<string, string?>("Page (px)", $"{preview.PageWidthPx} x {preview.PageHeightPx}"),
            });
        }
        else if (result.Data is not null)
        {
            AnsiConsole.MarkupLine($"Preview data: {result.Data}");
        }

        CliOutput.WriteIssues(result.Issues);
    }
}
