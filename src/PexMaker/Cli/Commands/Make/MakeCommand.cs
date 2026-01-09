using PexMaker.API;
using PexMaker.Cli.Helpers;
using PexMaker.Cli.Infrastructure;
using PexMaker.Engine.Application;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Commands.Make;

internal sealed class MakeCommand : AsyncCommand<MakeCommand.Settings>
{
    private readonly IPexMakerApiFactory _factory;

    public MakeCommand(IPexMakerApiFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<PROJECT_ID>")]
        public string ProjectId { get; init; } = string.Empty;

        [CommandOption("--name <DISPLAY>")]
        public string? DisplayName { get; init; }

        [CommandOption("--front-dir <DIR>")]
        public string FrontDir { get; init; } = string.Empty;

        [CommandOption("--back <FILE>")]
        public string? BackFile { get; init; }

        [CommandOption("--pairs <N>")]
        public int? PairCount { get; init; }

        [CommandOption("--dpi <N>")]
        public int? Dpi { get; init; }

        [CommandOption("--preset <ID>")]
        public string? PresetId { get; init; }

        [CommandOption("--format <png|pdf>")]
        public string Format { get; init; } = "pdf";

        [CommandOption("--out <DIR>")]
        public string? OutputDir { get; init; }

        [CommandOption("--bleed <MM>")]
        public double BleedMm { get; init; }

        [CommandOption("--safe <MM>")]
        public double SafeAreaMm { get; init; }

        [CommandOption("--cut-marks")]
        public bool IncludeCutMarks { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.FrontDir))
            {
                throw new CommandException("--front-dir is required.");
            }

            if (settings.PairCount is null)
            {
                throw new CommandException("--pairs is required.");
            }

            var api = _factory.Create(settings.Workspace);
            var projectPaths = api.EnsureProject(settings.ProjectId, settings.DisplayName);
            var mappingMode = MappingModeParser.Parse(settings.Mapping);

            var frontResult = await api.AddFrontImagesAsync(
                settings.ProjectId,
                new[] { settings.FrontDir },
                recursive: false,
                copy: true,
                context.CancellationToken).ConfigureAwait(false);

            ImportResult? backResult = null;
            var hasBack = !string.IsNullOrWhiteSpace(settings.BackFile);
            if (hasBack)
            {
                backResult = await api.SetBackImageAsync(
                    settings.ProjectId,
                    settings.BackFile!,
                    copy: true,
                    context.CancellationToken).ConfigureAwait(false);

                if (backResult.Added == 0)
                {
                    if (settings.JsonOutput)
                    {
                        CliOutput.WriteJson(BuildMakePayload(projectPaths, frontResult, backResult, null, null, null));
                        return 2;
                    }

                    WriteImportSections(frontResult, backResult);
                    return 2;
                }
            }

            var buildOptions = new BuildProjectOptions(
                settings.PairCount,
                settings.Dpi,
                settings.PresetId,
                settings.IncludeCutMarks,
                settings.BleedMm,
                settings.SafeAreaMm);

            var buildResult = await api.BuildEngineProjectAsync(
                settings.ProjectId,
                buildOptions,
                context.CancellationToken).ConfigureAwait(false);

            if (!buildResult.Succeeded)
            {
                if (settings.JsonOutput)
                {
                    CliOutput.WriteJson(BuildMakePayload(projectPaths, frontResult, backResult, buildResult, null, null));
                    return 2;
                }

                WriteImportSections(frontResult, backResult);
                WriteBuildSection(buildResult);
                return 2;
            }

            var projectJson = await File.ReadAllTextAsync(buildResult.ProjectJsonPath, context.CancellationToken)
                .ConfigureAwait(false);
            var validationResult = await api.ValidateProjectJsonAsync(
                projectJson,
                mappingMode,
                context.CancellationToken).ConfigureAwait(false);

            if (!validationResult.Succeeded)
            {
                if (settings.JsonOutput)
                {
                    CliOutput.WriteJson(BuildMakePayload(projectPaths, frontResult, backResult, buildResult, validationResult, null));
                    return 2;
                }

                WriteImportSections(frontResult, backResult);
                WriteBuildSection(buildResult);
                WriteValidationSection(validationResult);
                return 2;
            }

            var outputDir = settings.OutputDir ?? projectPaths.ExportDir;
            Directory.CreateDirectory(outputDir);

            var request = new ExportRequest
            {
                OutputDirectory = outputDir,
                Format = settings.Format,
                IncludeFront = true,
                IncludeBack = hasBack,
            };

            var exportResult = await api.ExportProjectAsync(
                settings.ProjectId,
                request,
                mappingMode,
                context.CancellationToken).ConfigureAwait(false);

            if (!exportResult.Succeeded)
            {
                if (settings.JsonOutput)
                {
                    CliOutput.WriteJson(BuildMakePayload(projectPaths, frontResult, backResult, buildResult, validationResult, exportResult));
                    return 2;
                }

                WriteImportSections(frontResult, backResult);
                WriteBuildSection(buildResult);
                WriteValidationSection(validationResult);
                WriteExportSection(exportResult);
                return 2;
            }

            if (settings.JsonOutput)
            {
                CliOutput.WriteJson(BuildMakePayload(projectPaths, frontResult, backResult, buildResult, validationResult, exportResult));
                return 0;
            }

            WriteImportSections(frontResult, backResult);
            WriteBuildSection(buildResult);
            WriteValidationSection(validationResult);
            WriteExportSection(exportResult);
            WriteSuccessSummary(exportResult);
            return 0;
        }
        catch (CommandException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CommandException(ex.Message);
        }
    }

    private static object BuildMakePayload(
        ProjectPaths projectPaths,
        ImportResult frontResult,
        ImportResult? backResult,
        BuildProjectResult? buildResult,
        ApiOperationResult? validationResult,
        ApiOperationResult? exportResult)
    {
        return new
        {
            Project = projectPaths,
            FrontImport = frontResult,
            BackImport = backResult,
            Build = buildResult,
            Validation = validationResult,
            Export = exportResult,
        };
    }

    private static void WriteImportSections(ImportResult frontResult, ImportResult? backResult)
    {
        var table = new Table
        {
            Title = new TableTitle("Front images"),
        };
        table.AddColumn("Added");
        table.AddColumn("Files");
        if (frontResult.Files.Count == 0)
        {
            table.AddRow(frontResult.Added.ToString(), "(none)");
        }
        else
        {
            var first = true;
            foreach (var file in frontResult.Files)
            {
                table.AddRow(first ? frontResult.Added.ToString() : string.Empty, file);
                first = false;
            }
        }

        AnsiConsole.Write(table);
        CliOutput.WriteIssues(frontResult.Issues);

        if (backResult is null)
        {
            return;
        }

        var backTable = new Table
        {
            Title = new TableTitle("Back image"),
        };
        backTable.AddColumn("Added");
        backTable.AddColumn("File");
        if (backResult.Files.Count == 0)
        {
            backTable.AddRow(backResult.Added.ToString(), "(none)");
        }
        else
        {
            backTable.AddRow(backResult.Added.ToString(), backResult.Files[0]);
        }

        AnsiConsole.Write(backTable);
        CliOutput.WriteIssues(backResult.Issues);
    }

    private static void WriteBuildSection(BuildProjectResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Build: {status}");
        var table = new Table();
        table.AddColumn("Project JSON");
        table.AddRow(result.ProjectJsonPath);
        AnsiConsole.Write(table);
        CliOutput.WriteIssues(result.Issues);
    }

    private static void WriteValidationSection(ApiOperationResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Validation: {status}");
        CliOutput.WriteIssues(result.Issues);
    }

    private static void WriteExportSection(ApiOperationResult result)
    {
        var status = result.Succeeded ? "[green]Succeeded[/]" : "[red]Failed[/]";
        AnsiConsole.MarkupLine($"Export: {status}");
        CliOutput.WriteIssues(result.Issues);
    }

    private static void WriteSuccessSummary(ApiOperationResult result)
    {
        if (result.Data is not ExportResult export)
        {
            return;
        }

        var table = new Table
        {
            Title = new TableTitle("Exported files"),
        };
        table.AddColumn("File");
        if (export.ProducedFiles.Count == 0)
        {
            table.AddRow("(none)");
        }
        else
        {
            foreach (var file in export.ProducedFiles)
            {
                table.AddRow(file);
            }
        }

        AnsiConsole.Write(table);
    }
}
