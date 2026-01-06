namespace PexMaker.Engine.Application.Mapping;

public enum MappingIssueSeverity
{
    Error,
    Warning,
}

public enum MappingIssueCode
{
    MissingValue,
    InvalidValue,
    UnsupportedSchemaVersion,
    UnknownEnum,
    DefaultApplied,
    ClampedValue,
}

public sealed record MappingIssue(MappingIssueCode Code, string Message, string? Path, MappingIssueSeverity Severity)
{
    public static MappingIssue Warning(MappingIssueCode code, string message, string? path = null) => new(code, message, path, MappingIssueSeverity.Warning);

    public static MappingIssue Error(MappingIssueCode code, string message, string? path = null) => new(code, message, path, MappingIssueSeverity.Error);
}
