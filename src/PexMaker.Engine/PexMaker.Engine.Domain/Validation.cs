namespace PexMaker.Engine.Domain;

public sealed record ValidationError(EngineErrorCode Code, string Message, string? Path = null);

public sealed class ProjectValidationResult
{
    public ProjectValidationResult(IEnumerable<ValidationError>? errors = null, IEnumerable<ValidationError>? warnings = null)
    {
        Errors = errors?.ToArray() ?? Array.Empty<ValidationError>();
        Warnings = warnings?.ToArray() ?? Array.Empty<ValidationError>();
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<ValidationError> Errors { get; }

    public IReadOnlyList<ValidationError> Warnings { get; }

    public static ProjectValidationResult Success(IEnumerable<ValidationError>? warnings = null) => new(null, warnings);

    public static ProjectValidationResult Failure(IEnumerable<ValidationError> errors, IEnumerable<ValidationError>? warnings = null) => new(errors, warnings);
}
