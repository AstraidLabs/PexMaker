using System.Text.Json;
using PexMaker.API;
using Spectre.Console;

namespace PexMaker.Cli.Helpers;

internal static class CliOutput
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static void WriteJson(object value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        AnsiConsole.WriteLine(json);
    }

    public static void WriteIssues(IEnumerable<ApiIssue> issues)
    {
        var list = issues?.ToList() ?? new List<ApiIssue>();
        if (list.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No issues found.[/]");
            return;
        }

        WriteIssueGroup(ApiSeverity.Error, list);
        WriteIssueGroup(ApiSeverity.Warning, list);
        WriteIssueGroup(ApiSeverity.Info, list);
    }

    public static void WriteKeyValueTable(string title, IEnumerable<KeyValuePair<string, string?>> rows)
    {
        var table = new Table
        {
            Title = new TableTitle(title),
        };

        table.AddColumn("Key");
        table.AddColumn("Value");

        foreach (var row in rows)
        {
            table.AddRow(row.Key, row.Value ?? string.Empty);
        }

        AnsiConsole.Write(table);
    }

    private static void WriteIssueGroup(ApiSeverity severity, IReadOnlyList<ApiIssue> issues)
    {
        var group = issues.Where(issue => issue.Severity == severity).ToList();
        if (group.Count == 0)
        {
            return;
        }

        AnsiConsole.Write(new Rule($"{severity} issues"));

        var table = new Table();
        table.AddColumn("Code");
        table.AddColumn("Message");
        table.AddColumn("Path");
        table.AddColumn("Recommendation");

        foreach (var issue in group)
        {
            table.AddRow(
                issue.Code,
                issue.Message,
                issue.Path ?? string.Empty,
                issue.Recommendation ?? string.Empty);
        }

        AnsiConsole.Write(table);
    }
}
