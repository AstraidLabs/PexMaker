using System;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed class ExportRequest
{
    public bool EnableParallelism { get; init; } = false;

    public int MaxDegreeOfParallelism { get; init; } = Math.Min(Environment.ProcessorCount, 4);

    public int MaxBufferedPages { get; init; } = 1;

    public int MaxBufferedCards { get; init; } = 64;

    public int MaxCacheItems { get; init; } = 256;

    public long MaxEstimatedWorkingSetBytes { get; init; } = 512L * 1024 * 1024;

    public bool IncludeFront { get; init; } = true;

    public bool IncludeBack { get; init; } = true;

    public int? Seed { get; init; }

    public string OutputDirectory { get; init; } = "out";

    public string NamingPrefixFront { get; init; } = "front";

    public string NamingPrefixBack { get; init; } = "back";

    public string Format { get; init; } = "png";

    public IProgress<EngineProgress>? Progress { get; init; }
}

public sealed class ExportResult
{
    public ExportResult(bool succeeded, IReadOnlyList<string> files, ProjectValidationResult validationResult)
    {
        Succeeded = succeeded;
        Files = files;
        ValidationResult = validationResult;
        Issues = validationResult.Errors.Concat(validationResult.Warnings).ToArray();
    }

    public bool Succeeded { get; }

    public IReadOnlyList<string> Files { get; }

    public ProjectValidationResult ValidationResult { get; }

    public bool IsSuccess => Succeeded;

    public IReadOnlyList<string> ProducedFiles => Files;

    public IReadOnlyList<ValidationError> Issues { get; }
}
