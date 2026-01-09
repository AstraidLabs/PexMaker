using System;

namespace PexMaker.Cli;

internal sealed class CommandException : Exception
{
    public CommandException(string message)
        : base(message)
    {
    }
}
