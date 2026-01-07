using System.Linq;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Abstractions;

public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    Stream OpenRead(string path);

    Stream OpenWrite(string path);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern);
}

public interface IRandomSource
{
    int NextInt(int maxExclusive);
}

public interface IRandomProvider
{
    IRandomSource Create(int? seed = null);
}

public enum MappingMode
{
    Strict,
    Lenient,
}

public enum IssueSeverity
{
    Error,
    Warning,
}

public sealed record SerializationIssue(string Code, string Message, string? Path, IssueSeverity Severity);

public sealed record SerializationResult(IReadOnlyList<SerializationIssue> Issues)
{
    public bool IsSuccess => Issues.All(issue => issue.Severity != IssueSeverity.Error);

    public static SerializationResult Success(IReadOnlyList<SerializationIssue>? issues = null) => new(issues ?? Array.Empty<SerializationIssue>());
}

public sealed record DeserializationResult(PexProject? Project, IReadOnlyList<SerializationIssue> Issues)
{
    public bool IsSuccess => Project is not null && Issues.All(issue => issue.Severity != IssueSeverity.Error);
}

public interface IProjectSerializer
{
    Task<SerializationResult> SaveAsync(PexProject project, Stream destination, CancellationToken cancellationToken);

    Task<DeserializationResult> LoadAsync(Stream source, MappingMode mode, CancellationToken cancellationToken);
}
