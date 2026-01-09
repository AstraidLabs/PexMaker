using System.Text;
using PexMaker.API.Placeholder;
using PexMaker.API.Workspace;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Application;
using PexMaker.Engine.Domain;
using PexMaker.Engine.Infrastructure;

namespace PexMaker.API;

/// <summary>
/// Provides a simple API for working with PexMaker workspaces and engine operations.
/// </summary>
public sealed class PexMakerApi : IPexMakerApi
{
    private readonly WorkspaceManager _workspace;
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    /// <summary>
    /// Creates a new API instance.
    /// </summary>
    /// <param name="options">Workspace options.</param>
    public PexMakerApi(WorkspaceOptions? options = null)
    {
        _workspace = new WorkspaceManager(options ?? new WorkspaceOptions());
    }

    /// <inheritdoc />
    public WorkspacePaths GetWorkspacePaths() => _workspace.GetWorkspacePaths();

    /// <inheritdoc />
    public IReadOnlyList<string> ListProjects() => _workspace.ListProjects();

    /// <inheritdoc />
    public ProjectPaths EnsureProject(string projectId, string? displayName = null)
        => _workspace.EnsureProject(projectId, displayName);

    /// <inheritdoc />
    public ProjectPlaceholder GetPlaceholder(string projectId)
        => _workspace.GetPlaceholder(projectId);

    /// <inheritdoc />
    public ProjectPlaceholder SavePlaceholder(string projectId, ProjectPlaceholder placeholder)
        => _workspace.SavePlaceholder(projectId, placeholder);

    /// <inheritdoc />
    public Task<ImportResult> AddFrontImagesAsync(string projectId, IEnumerable<string> inputPaths, bool recursive, bool copy, CancellationToken ct)
    {
        var issues = new List<ApiIssue>();
        var paths = EnsureProject(projectId, null);

        var sources = CollectInputFiles(inputPaths, recursive, issues);
        if (sources.Count == 0)
        {
            return Task.FromResult(new ImportResult(0, Array.Empty<string>(), issues));
        }

        var existingNames = new HashSet<string>(
            Directory.EnumerateFiles(paths.FrontDir)
                .Select(Path.GetFileName)
                .OfType<string>(),
            NameComparer);

        var addedFiles = new List<string>();
        foreach (var source in sources)
        {
            ct.ThrowIfCancellationRequested();
            var baseName = Path.GetFileNameWithoutExtension(source);
            var extension = Path.GetExtension(source);
            var sanitizedBase = SanitizeFileNameBase(baseName);
            var destinationName = GetUniqueFileName(sanitizedBase, extension, existingNames);
            var destinationPath = Path.Combine(paths.FrontDir, destinationName);

            try
            {
                if (copy)
                {
                    File.Copy(source, destinationPath, overwrite: false);
                }
                else
                {
                    File.Move(source, destinationPath);
                }

                addedFiles.Add(Path.GetFullPath(destinationPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                issues.Add(new ApiIssue(
                    "ImportFailed",
                    ApiSeverity.Error,
                    $"Failed to import '{source}': {ex.Message}",
                    source));
            }
        }

        addedFiles.Sort(StringComparer.Ordinal);
        return Task.FromResult(new ImportResult(addedFiles.Count, addedFiles, issues));
    }

    /// <inheritdoc />
    public Task<ImportResult> SetBackImageAsync(string projectId, string inputFile, bool copy, CancellationToken ct)
    {
        var issues = new List<ApiIssue>();
        var paths = EnsureProject(projectId, null);

        if (string.IsNullOrWhiteSpace(inputFile))
        {
            issues.Add(new ApiIssue("InputPathMissing", ApiSeverity.Error, "Back image path is required."));
            return Task.FromResult(new ImportResult(0, Array.Empty<string>(), issues));
        }

        if (!File.Exists(inputFile))
        {
            issues.Add(new ApiIssue("InputPathMissing", ApiSeverity.Error, $"Input file not found: {inputFile}", inputFile));
            return Task.FromResult(new ImportResult(0, Array.Empty<string>(), issues));
        }

        if (!IsSupportedExtension(inputFile))
        {
            issues.Add(new ApiIssue("UnsupportedExtension", ApiSeverity.Warning, $"Unsupported file extension: {inputFile}", inputFile));
            return Task.FromResult(new ImportResult(0, Array.Empty<string>(), issues));
        }

        foreach (var file in Directory.EnumerateFiles(paths.BackDir))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                issues.Add(new ApiIssue(
                    "DeleteFailed",
                    ApiSeverity.Warning,
                    $"Failed to delete existing back image '{file}': {ex.Message}",
                    file));
            }
        }

        var extension = Path.GetExtension(inputFile);
        var destinationPath = Path.Combine(paths.BackDir, $"back{extension}");
        try
        {
            if (copy)
            {
                File.Copy(inputFile, destinationPath, overwrite: false);
            }
            else
            {
                File.Move(inputFile, destinationPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            issues.Add(new ApiIssue(
                "ImportFailed",
                ApiSeverity.Error,
                $"Failed to import '{inputFile}': {ex.Message}",
                inputFile));
            return Task.FromResult(new ImportResult(0, Array.Empty<string>(), issues));
        }

        var fullPath = Path.GetFullPath(destinationPath);
        return Task.FromResult(new ImportResult(1, new[] { fullPath }, issues));
    }

    /// <inheritdoc />
    public async Task<BuildProjectResult> BuildEngineProjectAsync(string projectId, BuildProjectOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);

        var issues = new List<ApiIssue>();
        var paths = EnsureProject(projectId, null);
        var placeholder = GetPlaceholder(projectId);

        var projectJsonPath = Path.GetFullPath(Path.Combine(paths.ProjectRoot, "project.json"));
        var pairCount = options.PairCount ?? placeholder.PairCountHint;
        var dpi = options.Dpi ?? placeholder.DpiHint;

        if (pairCount < 1)
        {
            issues.Add(new ApiIssue("PairCountInvalid", ApiSeverity.Error, "Pair count must be at least 1."));
        }

        if (dpi <= 0)
        {
            issues.Add(new ApiIssue("DpiInvalid", ApiSeverity.Error, "DPI must be greater than 0."));
        }

        var frontImages = EnumerateImageFiles(paths.FrontDir)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (frontImages.Count < pairCount)
        {
            issues.Add(new ApiIssue(
                "FrontImagesMissing",
                ApiSeverity.Error,
                $"Not enough front images. Needed {pairCount}, found {frontImages.Count}."));
        }

        if (issues.Any(issue => issue.Severity == ApiSeverity.Error))
        {
            return new BuildProjectResult(false, projectJsonPath, issues);
        }

        var selectedFrontImages = frontImages
            .Take(pairCount)
            .Select(path => new ImageRef { Path = Path.GetFullPath(path) })
            .ToList();

        var backImagePath = EnumerateImageFiles(paths.BackDir)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ThenBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();

        var project = new PexProject
        {
            PairCount = pairCount,
            FrontImages = selectedFrontImages,
            BackImage = backImagePath is null ? null : new ImageRef { Path = Path.GetFullPath(backImagePath) },
            Layout = new LayoutOptions(),
            Dpi = Dpi.From(dpi),
        };

        var engine = CreateEngine();
        if (!string.IsNullOrWhiteSpace(options.PresetId))
        {
            if (engine.TryGetPresetById(options.PresetId, out var preset))
            {
                project = engine.ApplyPreset(project, preset.Id);
            }
            else
            {
                issues.Add(new ApiIssue(
                    "PresetNotFound",
                    ApiSeverity.Warning,
                    $"Preset '{options.PresetId}' was not found."));
            }
        }
        else
        {
            var defaultPreset = engine.GetPresets()
                .OrderBy(preset => preset.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (defaultPreset is not null)
            {
                project = engine.ApplyPreset(project, defaultPreset.Id);
            }
        }

        var layout = project.Layout with
        {
            Bleed = Mm.From(options.BleedMm),
            CutMarks = options.IncludeCutMarks,
        };

        if (typeof(LayoutOptions).GetProperty(nameof(LayoutOptions.SafeArea)) is not null)
        {
            layout = layout with { SafeArea = Mm.From(options.SafeAreaMm) };
        }
        else
        {
            issues.Add(new ApiIssue(
                "SafeAreaUnsupported",
                ApiSeverity.Warning,
                "Safe area is not supported by the current engine."));
        }

        project = new PexProject
        {
            PairCount = project.PairCount,
            FrontImages = project.FrontImages,
            BackImage = project.BackImage,
            Layout = layout,
            Dpi = project.Dpi,
        };

        await using var stream = new FileStream(projectJsonPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var saveResult = await engine.SaveProjectAsync(project, stream, ct).ConfigureAwait(false);
        issues.AddRange(MapValidationResult(saveResult.ValidationResult));

        if (!saveResult.ValidationResult.IsValid)
        {
            return new BuildProjectResult(false, projectJsonPath, issues);
        }

        return new BuildProjectResult(true, projectJsonPath, issues);
    }

    /// <inheritdoc />
    public async Task<ApiOperationResult> ExportProjectAsync(string projectId, ExportRequest request, MappingMode mappingMode, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paths = EnsureProject(projectId, null);
        Directory.CreateDirectory(request.OutputDirectory);
        var projectJsonPath = Path.Combine(paths.ProjectRoot, "project.json");
        if (!File.Exists(projectJsonPath))
        {
            return new ApiOperationResult(false, null, new[]
            {
                new ApiIssue("ProjectJsonMissing", ApiSeverity.Error, "Project JSON does not exist.", projectJsonPath),
            });
        }

        var json = await File.ReadAllTextAsync(projectJsonPath, ct).ConfigureAwait(false);
        return await ExportProjectJsonAsync(json, request, mappingMode, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<LayoutPresetInfo> GetPresets()
    {
        var engine = CreateEngine();
        return engine.GetPresets()
            .OrderBy(preset => preset.Id, StringComparer.Ordinal)
            .Select(preset => new LayoutPresetInfo(preset.Id, preset.Name, null))
            .ToArray();
    }

    /// <summary>
    /// Validates project JSON with lenient mapping.
    /// </summary>
    public Task<ApiOperationResult> ValidateProjectJsonAsync(string projectJson, CancellationToken ct)
        => ValidateProjectJsonAsync(projectJson, MappingMode.Lenient, ct);

    /// <summary>
    /// Previews project JSON with lenient mapping.
    /// </summary>
    public Task<ApiOperationResult> PreviewProjectJsonAsync(string projectJson, string? presetId, CancellationToken ct)
        => PreviewProjectJsonAsync(projectJson, presetId, MappingMode.Lenient, ct);

    /// <summary>
    /// Exports project JSON with lenient mapping.
    /// </summary>
    public Task<ApiOperationResult> ExportProjectJsonAsync(string projectJson, ExportRequest request, CancellationToken ct)
        => ExportProjectJsonAsync(projectJson, request, MappingMode.Lenient, ct);

    /// <inheritdoc />
    public async Task<ApiOperationResult> ValidateProjectJsonAsync(string projectJson, MappingMode mappingMode, CancellationToken ct)
    {
        var loadResult = await LoadProjectAsync(projectJson, mappingMode, ct).ConfigureAwait(false);
        if (!loadResult.Succeeded)
        {
            return new ApiOperationResult(false, null, loadResult.Issues);
        }

        var engine = CreateEngine();
        var validationResult = await engine.ValidateAsync(loadResult.Project!, ct).ConfigureAwait(false);
        var issues = loadResult.Issues.Concat(MapValidationResult(validationResult)).ToArray();
        return new ApiOperationResult(validationResult.IsValid, validationResult, issues);
    }

    /// <inheritdoc />
    public async Task<ApiOperationResult> PreviewProjectJsonAsync(string projectJson, string? presetId, MappingMode mappingMode, CancellationToken ct)
    {
        var loadResult = await LoadProjectAsync(projectJson, mappingMode, ct).ConfigureAwait(false);
        if (!loadResult.Succeeded)
        {
            return new ApiOperationResult(false, null, loadResult.Issues);
        }

        var engine = CreateEngine();
        var preview = engine.PreviewLayout(loadResult.Project!, presetId);
        var issues = loadResult.Issues.Concat(MapValidationResult(loadResult.ValidationResult)).ToArray();
        return new ApiOperationResult(true, preview, issues);
    }

    /// <inheritdoc />
    public async Task<ApiOperationResult> ExportProjectJsonAsync(string projectJson, ExportRequest request, MappingMode mappingMode, CancellationToken ct)
    {
        var loadResult = await LoadProjectAsync(projectJson, mappingMode, ct).ConfigureAwait(false);
        if (!loadResult.Succeeded)
        {
            return new ApiOperationResult(false, null, loadResult.Issues);
        }

        var engine = CreateEngine();
        var exportResult = await engine.ExportAsync(loadResult.Project!, request, ct).ConfigureAwait(false);
        var issues = loadResult.Issues.Concat(MapValidationResult(exportResult.ValidationResult)).ToArray();
        return new ApiOperationResult(exportResult.Succeeded, exportResult, issues);
    }

    internal static PexMakerEngine CreateEngine() => PexMakerEngineFactory.CreateDefault();

    private static async Task<LoadProjectResult> LoadProjectAsync(string projectJson, MappingMode mappingMode, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(projectJson);

        var engine = CreateEngine();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(projectJson));
        var result = await engine.LoadProjectAsync(stream, mappingMode, ct).ConfigureAwait(false);

        if (result.Value?.Project is null)
        {
            return LoadProjectResult.Failure(new ApiIssue(
                "ProjectJsonInvalid",
                ApiSeverity.Error,
                "Project JSON could not be loaded."));
        }

        var issues = MapSerializationIssues(result.Value.Issues).ToList();
        return LoadProjectResult.Success(result.Value.Project, result.ValidationResult, issues);
    }

    private static IEnumerable<ApiIssue> MapSerializationIssues(IEnumerable<SerializationIssue> issues)
    {
        foreach (var issue in issues)
        {
            yield return new ApiIssue(
                issue.Code,
                issue.Severity == IssueSeverity.Warning ? ApiSeverity.Warning : ApiSeverity.Error,
                issue.Message,
                issue.Path);
        }
    }

    private static IEnumerable<ApiIssue> MapValidationResult(ProjectValidationResult validationResult)
        => validationResult.Issues.Select(MapValidationIssue);

    private static ApiIssue MapValidationIssue(ValidationError issue)
        => new(
            issue.Code.ToString(),
            issue.Severity == ValidationSeverity.Warning ? ApiSeverity.Warning : ApiSeverity.Error,
            issue.Message,
            issue.Field);

    private sealed record LoadProjectResult(
        PexProject? Project,
        ProjectValidationResult ValidationResult,
        IReadOnlyList<ApiIssue> Issues,
        bool Succeeded)
    {
        internal static LoadProjectResult Failure(ApiIssue issue)
            => new(null, ProjectValidationResult.Failure(new[] { new ValidationError(EngineErrorCode.ProjectLoadFailed, issue.Message, issue.Path) }), new[] { issue }, false);

        internal static LoadProjectResult Success(PexProject project, ProjectValidationResult validationResult, IReadOnlyList<ApiIssue> issues)
            => new(project, validationResult, issues, true);
    }

    private static IReadOnlyList<string> CollectInputFiles(IEnumerable<string> inputPaths, bool recursive, List<ApiIssue> issues)
    {
        if (inputPaths is null)
        {
            issues.Add(new ApiIssue("InputPathsMissing", ApiSeverity.Error, "Input paths are required."));
            return Array.Empty<string>();
        }

        var files = new List<string>();
        foreach (var input in inputPaths)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                issues.Add(new ApiIssue("InputPathMissing", ApiSeverity.Warning, "Input path is empty."));
                continue;
            }

            if (Directory.Exists(input))
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var file in Directory.EnumerateFiles(input, "*", searchOption))
                {
                    if (IsSupportedExtension(file))
                    {
                        files.Add(Path.GetFullPath(file));
                    }
                    else
                    {
                        issues.Add(new ApiIssue(
                            "UnsupportedExtension",
                            ApiSeverity.Warning,
                            $"Unsupported file extension: {file}",
                            file));
                    }
                }

                continue;
            }

            if (File.Exists(input))
            {
                if (IsSupportedExtension(input))
                {
                    files.Add(Path.GetFullPath(input));
                }
                else
                {
                    issues.Add(new ApiIssue(
                        "UnsupportedExtension",
                        ApiSeverity.Warning,
                        $"Unsupported file extension: {input}",
                        input));
                }

                continue;
            }

            issues.Add(new ApiIssue("InputPathMissing", ApiSeverity.Error, $"Input path not found: {input}", input));
        }

        return files
            .Distinct(PathComparer)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateImageFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(directory)
            .Where(IsSupportedExtension);
    }

    private static bool IsSupportedExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeFileNameBase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "image";
        }

        var builder = new StringBuilder(name.Length);
        var lastDash = false;

        foreach (var ch in name)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastDash)
                {
                    builder.Append('-');
                    lastDash = true;
                }

                continue;
            }

            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')
            {
                if (ch == '-')
                {
                    if (!lastDash)
                    {
                        builder.Append('-');
                        lastDash = true;
                    }
                }
                else
                {
                    builder.Append(ch);
                    lastDash = false;
                }
            }
        }

        var sanitized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized;
    }

    private static string GetUniqueFileName(string baseName, string extension, HashSet<string> usedNames)
    {
        var initial = $"{baseName}{extension}";
        if (usedNames.Add(initial))
        {
            return initial;
        }

        for (var i = 1; ; i++)
        {
            var candidate = $"{baseName}-{i:000}{extension}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }
    }
}
