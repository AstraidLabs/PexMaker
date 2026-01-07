namespace PexMaker.Engine.Application;

public enum EvaluationSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record EvaluationIssue(
    string Code,
    EvaluationSeverity Severity,
    string Message,
    string? Path = null,
    string? Recommendation = null);

public sealed record EvaluationMetric(string Name, string Value, string? Unit = null);

public sealed record EvaluationReport(
    IReadOnlyList<EvaluationIssue> Issues,
    IReadOnlyList<EvaluationMetric> Metrics,
    bool IsLayoutSafe,
    bool IsQualityOk);
