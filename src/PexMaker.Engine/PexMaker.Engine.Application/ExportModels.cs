using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed class ExportRequest
{
    public string OutputDirectory { get; init; } = "output";

    public ExportImageFormat Format { get; init; } = ExportImageFormat.Png;

    public string FrontPrefix { get; init; } = "front";

    public string BackPrefix { get; init; } = "back";

    public int? Seed { get; init; }
}

public sealed class ExportResult
{
    public ExportResult(bool succeeded, IReadOnlyList<string> files, ProjectValidationResult validationResult)
    {
        Succeeded = succeeded;
        Files = files;
        ValidationResult = validationResult;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<string> Files { get; }

    public ProjectValidationResult ValidationResult { get; }
}
