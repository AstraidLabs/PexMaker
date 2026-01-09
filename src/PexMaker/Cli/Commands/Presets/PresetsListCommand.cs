using PexMaker.API;
using PexMaker.Cli;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Presets;

internal sealed class PresetsListCommand : Command<PresetsListCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public PresetsListCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var api = _factory.Create(settings.Workspace);
            var presets = api.GetPresets();

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(presets);
            }
            else
            {
                WritePresetTable(presets);
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

    private static void WritePresetTable(IReadOnlyList<LayoutPresetInfo> presets)
    {
        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Description");

        if (presets.Count == 0)
        {
            table.AddRow("(none)", string.Empty, string.Empty);
        }
        else
        {
            foreach (var preset in presets)
            {
                table.AddRow(preset.Id, preset.Name, preset.Description ?? string.Empty);
            }
        }

        AnsiConsole.Write(table);
    }
}
