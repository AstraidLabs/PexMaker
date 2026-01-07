using System.Linq;

namespace PexMaker.Engine.Domain;

public enum ValidationSeverity
{
    Error,
    Warning,
}

public sealed record ValidationError(
    EngineErrorCode Code,
    string Message,
    string? Field = null,
    ValidationSeverity Severity = ValidationSeverity.Error);

public sealed class ProjectValidationResult
{
    public ProjectValidationResult(IEnumerable<ValidationError>? errors = null, IEnumerable<ValidationError>? warnings = null)
    {
        var errorItems = errors?.Select(error => error with { Severity = ValidationSeverity.Error })
            ?? Enumerable.Empty<ValidationError>();
        var warningItems = warnings?.Select(warning => warning with { Severity = ValidationSeverity.Warning })
            ?? Enumerable.Empty<ValidationError>();

        Issues = errorItems.Concat(warningItems).ToArray();
        Errors = Issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();
        Warnings = Issues.Where(issue => issue.Severity == ValidationSeverity.Warning).ToArray();
    }

    private ProjectValidationResult(IReadOnlyList<ValidationError> issues)
    {
        Issues = issues;
        Errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();
        Warnings = issues.Where(issue => issue.Severity == ValidationSeverity.Warning).ToArray();
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<ValidationError> Issues { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public IReadOnlyList<ValidationError> Warnings { get; }

    public static ProjectValidationResult Success(IEnumerable<ValidationError>? warnings = null) => new(null, warnings);

    public static ProjectValidationResult Failure(IEnumerable<ValidationError> errors, IEnumerable<ValidationError>? warnings = null) => new(errors, warnings);

    public static ProjectValidationResult FromIssues(IEnumerable<ValidationError> issues)
        => new(issues.ToArray());

    public static ProjectValidationResult Combine(params ProjectValidationResult[] results)
    {
        if (results.Length == 0)
        {
            return Success();
        }

        var combined = results.SelectMany(result => result.Issues).ToArray();
        return FromIssues(combined);
    }
}
