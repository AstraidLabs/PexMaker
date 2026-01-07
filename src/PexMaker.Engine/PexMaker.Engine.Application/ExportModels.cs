using System;
using System.Linq;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed class ExportRequest
{
    /// <summary>
    /// Enables parallelism in the export pipeline.
    /// </summary>
    public bool EnableParallelism { get; init; } = false;

    /// <summary>
    /// Maximum degree of parallelism for rendering. Must stay within engine limits.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Math.Min(Environment.ProcessorCount, 4);

    /// <summary>
    /// Maximum number of buffered pages in the pipeline.
    /// </summary>
    public int MaxBufferedPages { get; init; } = 1;

    /// <summary>
    /// Maximum number of buffered cards in the pipeline.
    /// </summary>
    public int MaxBufferedCards { get; init; } = 64;

    /// <summary>
    /// Maximum number of cache items held for image decoding.
    /// </summary>
    public int MaxCacheItems { get; init; } = 256;

    /// <summary>
    /// Maximum estimated working set (bytes) for the export pipeline.
    /// </summary>
    public long MaxEstimatedWorkingSetBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>
    /// Whether to export front sides.
    /// </summary>
    public bool IncludeFront { get; init; } = true;

    /// <summary>
    /// Whether to export back sides.
    /// </summary>
    public bool IncludeBack { get; init; } = true;

    /// <summary>
    /// Optional seed for deterministic shuffling. Defaults to 0 when null.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Output directory. The engine requires it to exist and uses its canonical path; existing files are not overwritten.
    /// </summary>
    public string OutputDirectory { get; init; } = "out";

    /// <summary>
    /// File name prefix for front pages (whitelisted characters only).
    /// </summary>
    public string NamingPrefixFront { get; init; } = "front";

    /// <summary>
    /// File name prefix for back pages (whitelisted characters only).
    /// </summary>
    public string NamingPrefixBack { get; init; } = "back";

    /// <summary>
    /// Export image format. Only "png" is currently supported.
    /// </summary>
    public string Format { get; init; } = "png";

    /// <summary>
    /// Optional progress reporting sink.
    /// </summary>
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
