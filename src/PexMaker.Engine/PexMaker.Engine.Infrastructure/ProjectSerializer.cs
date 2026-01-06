using System.Linq;
using System.Text.Json;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Application.Mapping;
using PexMaker.Engine.Contracts;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Infrastructure;

internal sealed class JsonProjectSerializer : IProjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<SerializationResult> SaveAsync(PexProject project, Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<SerializationIssue>();
        var mapping = ProjectMapper.ToDto(project);
        issues.AddRange(mapping.Issues.Select(MapIssue));

        if (mapping.Value is null || mapping.Issues.Any(i => i.Severity == MappingIssueSeverity.Error))
        {
            return new SerializationResult(issues);
        }

        try
        {
            await JsonSerializer.SerializeAsync(destination, mapping.Value, Options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            issues.Add(new SerializationIssue("SerializationError", ex.Message, null, IssueSeverity.Error));
        }

        return new SerializationResult(issues);
    }

    public async Task<DeserializationResult> LoadAsync(Stream source, MappingMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<SerializationIssue>();
        PexProjectDto? dto;

        try
        {
            dto = await JsonSerializer.DeserializeAsync<PexProjectDto>(source, Options, cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                issues.Add(new SerializationIssue("DeserializationError", "Project payload was empty.", null, IssueSeverity.Error));
                return new DeserializationResult(null, issues);
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            issues.Add(new SerializationIssue("DeserializationError", ex.Message, null, IssueSeverity.Error));
            return new DeserializationResult(null, issues);
        }

        var mapping = ProjectMapper.ToDomain(dto, mode);
        issues.AddRange(mapping.Issues.Select(MapIssue));

        var project = mapping.Value;
        if (mode == MappingMode.Strict && mapping.Issues.Any(i => i.Severity == MappingIssueSeverity.Error))
        {
            project = null;
        }

        return new DeserializationResult(project, issues);
    }

    private static SerializationIssue MapIssue(MappingIssue issue)
    {
        var severity = issue.Severity == MappingIssueSeverity.Error ? IssueSeverity.Error : IssueSeverity.Warning;
        return new SerializationIssue(issue.Code.ToString(), issue.Message, issue.Path, severity);
    }
}
