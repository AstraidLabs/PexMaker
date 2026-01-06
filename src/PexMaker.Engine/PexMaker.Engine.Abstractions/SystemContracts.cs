using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Abstractions;

public interface IFileSystem
{
    bool FileExists(string path);

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

public interface IProjectSerializer
{
    Task SerializeAsync(PexProject project, Stream destination, CancellationToken cancellationToken);

    Task<PexProject> DeserializeAsync(Stream source, CancellationToken cancellationToken);
}
