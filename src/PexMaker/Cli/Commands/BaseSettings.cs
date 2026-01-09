using PexMaker.Cli.Helpers;
using PexMaker.Cli;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands;

internal abstract class BaseSettings : CommandSettings
{
    [CommandOption("--workspace <PATH>")]
    public string? Workspace { get; init; }

    [CommandOption("--json")]
    public bool JsonOutput { get; init; }

    [CommandOption("--mapping <MODE>")]
    public string? Mapping { get; init; }

    public override ValidationResult Validate()
    {
        if (!string.IsNullOrWhiteSpace(Mapping))
        {
            try
            {
                _ = MappingModeParser.Parse(Mapping);
            }
            catch (CommandException ex)
            {
                return ValidationResult.Error(ex.Message);
            }
        }

        return ValidationResult.Success();
    }
}
