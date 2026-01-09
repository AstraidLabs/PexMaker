using PexMaker.Engine.Abstractions;
using Spectre.Console.Cli;

using PexMaker.Cli;

namespace PexMaker.Cli.Helpers;

internal static class MappingModeParser
{
    public static MappingMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MappingMode.Lenient;
        }

        if (string.Equals(value, "lenient", StringComparison.OrdinalIgnoreCase))
        {
            return MappingMode.Lenient;
        }

        if (string.Equals(value, "strict", StringComparison.OrdinalIgnoreCase))
        {
            return MappingMode.Strict;
        }

        throw new CommandException($"Unknown mapping mode '{value}'. Supported values: lenient, strict.");
    }
}
