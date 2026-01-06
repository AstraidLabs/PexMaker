using System.Linq;

namespace PexMaker.Engine.Application.Mapping;

public sealed class MappingResult<T>
{
    public MappingResult(T? value, IReadOnlyList<MappingIssue> issues)
    {
        Value = value;
        Issues = issues ?? Array.Empty<MappingIssue>();
    }

    public T? Value { get; }

    public IReadOnlyList<MappingIssue> Issues { get; }

    public bool IsSuccess => Issues.All(issue => issue.Severity != MappingIssueSeverity.Error);
}
