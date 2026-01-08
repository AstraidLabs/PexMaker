using System.Text;
using System.Text.Json;
using PexMaker.API.Placeholder;

namespace PexMaker.API.Workspace;

internal sealed class WorkspaceManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly WorkspaceOptions _options;

    internal WorkspaceManager(WorkspaceOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ProjectsRoot = Path.Combine(_options.WorkspaceRoot, "projects");
    }

    internal string ProjectsRoot { get; }

    internal static string SanitizeProjectId(string projectId)
    {
        ArgumentNullException.ThrowIfNull(projectId);

        var builder = new StringBuilder(projectId.Length);
        foreach (var ch in projectId)
        {
            if (char.IsWhiteSpace(ch))
            {
                builder.Append('-');
                continue;
            }

            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
            {
                builder.Append(ch);
            }
        }

        var sanitized = builder.ToString().Trim('-', '_');
        return string.IsNullOrWhiteSpace(sanitized) ? "project" : sanitized;
    }

    internal WorkspacePaths GetWorkspacePaths()
    {
        var root = Path.GetFullPath(_options.WorkspaceRoot);
        var projectsRoot = Path.GetFullPath(ProjectsRoot);
        return new WorkspacePaths(root, projectsRoot);
    }

    internal IReadOnlyList<string> ListProjects()
    {
        if (!Directory.Exists(ProjectsRoot))
        {
            return Array.Empty<string>();
        }

        var directories = Directory.EnumerateDirectories(ProjectsRoot)
            .Select(path => new DirectoryInfo(path))
            .Where(info => !info.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System))
            .Select(info => info.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return directories;
    }

    internal ProjectPaths EnsureProject(string projectId, string? displayName)
    {
        var sanitizedId = SanitizeProjectId(projectId);

        Directory.CreateDirectory(ProjectsRoot);
        var projectRoot = Path.Combine(ProjectsRoot, sanitizedId);
        Directory.CreateDirectory(projectRoot);

        var frontDir = Path.Combine(projectRoot, "source", "front");
        var backDir = Path.Combine(projectRoot, "source", "back");
        var exportDir = Path.Combine(projectRoot, "export");
        var tempDir = Path.Combine(projectRoot, "tmp");

        Directory.CreateDirectory(frontDir);
        Directory.CreateDirectory(backDir);
        Directory.CreateDirectory(exportDir);
        Directory.CreateDirectory(tempDir);

        var placeholderPath = Path.Combine(projectRoot, PlaceholderSchema.FileName);
        if (!File.Exists(placeholderPath))
        {
            var nowUtc = DateTimeOffset.UtcNow.ToString("O");
            var placeholder = PlaceholderFactory.CreateNew(sanitizedId, displayName ?? sanitizedId, nowUtc);
            WritePlaceholder(placeholderPath, placeholder);
        }

        return new ProjectPaths(
            sanitizedId,
            Path.GetFullPath(projectRoot),
            Path.GetFullPath(placeholderPath),
            Path.GetFullPath(frontDir),
            Path.GetFullPath(backDir),
            Path.GetFullPath(exportDir),
            Path.GetFullPath(tempDir));
    }

    internal ProjectPlaceholder GetPlaceholder(string projectId)
    {
        var paths = EnsureProject(projectId, null);
        var json = File.ReadAllText(paths.PlaceholderPath, Encoding.UTF8);
        var placeholder = JsonSerializer.Deserialize<ProjectPlaceholder>(json, JsonOptions);
        if (placeholder is null)
        {
            throw new InvalidDataException("Project placeholder could not be loaded.");
        }

        if (!string.Equals(placeholder.Schema, PlaceholderSchema.Schema, StringComparison.Ordinal)
            || placeholder.Version != PlaceholderSchema.Version)
        {
            throw new InvalidDataException("Project placeholder schema/version mismatch.");
        }

        return placeholder;
    }

    internal ProjectPlaceholder SavePlaceholder(string projectId, ProjectPlaceholder placeholder)
    {
        ArgumentNullException.ThrowIfNull(placeholder);

        var paths = EnsureProject(projectId, placeholder.DisplayName);
        var placeholderPath = paths.PlaceholderPath;
        var nowUtc = DateTimeOffset.UtcNow.ToString("O");
        var createdUtc = placeholder.CreatedUtc;

        if (File.Exists(placeholderPath))
        {
            try
            {
                var existingJson = File.ReadAllText(placeholderPath, Encoding.UTF8);
                var existing = JsonSerializer.Deserialize<ProjectPlaceholder>(existingJson, JsonOptions);
                if (existing is not null && !string.IsNullOrWhiteSpace(existing.CreatedUtc))
                {
                    createdUtc = existing.CreatedUtc;
                }
            }
            catch (JsonException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        var finalPlaceholder = placeholder with
        {
            Schema = PlaceholderSchema.Schema,
            Version = PlaceholderSchema.Version,
            ProjectId = paths.ProjectId,
            CreatedUtc = createdUtc,
            UpdatedUtc = nowUtc,
        };

        WritePlaceholder(placeholderPath, finalPlaceholder);
        return finalPlaceholder;
    }

    private static void WritePlaceholder(string placeholderPath, ProjectPlaceholder placeholder)
    {
        var json = JsonSerializer.Serialize(placeholder, JsonOptions);
        File.WriteAllText(placeholderPath, json + "\n", Encoding.UTF8);
    }
}
