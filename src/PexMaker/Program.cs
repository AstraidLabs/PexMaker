using Microsoft.Extensions.DependencyInjection;
using PexMaker.Cli.Commands.Images;
using PexMaker.Cli.Commands.Make;
using PexMaker.Cli.Commands.Engine;
using PexMaker.Cli.Commands.Presets;
using PexMaker.Cli.Commands.Projects;
using PexMaker.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<IPexMakerApiFactory, PexMakerApiFactory>();
services.AddTransient<ProjectsListCommand>();
services.AddTransient<ProjectsInitCommand>();
services.AddTransient<ProjectsShowCommand>();
services.AddTransient<ProjectBuildCommand>();
services.AddTransient<EngineValidateCommand>();
services.AddTransient<EnginePreviewCommand>();
services.AddTransient<EngineExportCommand>();
services.AddTransient<ImagesAddFrontCommand>();
services.AddTransient<ImagesSetBackCommand>();
services.AddTransient<PresetsListCommand>();
services.AddTransient<MakeCommand>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("pexmaker");
    config.SetApplicationVersion("1.0.0");
    config.ValidateExamples();

    config.AddBranch("projects", projects =>
    {
        projects.SetDescription("Project workspace commands");
        projects.AddCommand<ProjectsListCommand>("list")
            .WithDescription("List projects")
            .WithExample(new[] { "projects", "list" });
        projects.AddCommand<ProjectsInitCommand>("init")
            .WithDescription("Initialize a project workspace")
            .WithExample(new[] { "projects", "init", "skolni-pexeso", "--name", "Pexeso do prvouky" });
        projects.AddCommand<ProjectsShowCommand>("show")
            .WithDescription("Show placeholder for a project")
            .WithExample(new[] { "projects", "show", "skolni-pexeso" });
    });

    config.AddBranch("engine", engine =>
    {
        engine.SetDescription("Engine operations using project JSON");
        engine.AddCommand<EngineValidateCommand>("validate")
            .WithDescription("Validate project JSON")
            .WithExample(new[] { "engine", "validate", "--file", "path/to/project.json" });
        engine.AddCommand<EnginePreviewCommand>("preview")
            .WithDescription("Preview layout")
            .WithExample(new[] { "engine", "preview", "--file", "path/to/project.json" });
        engine.AddCommand<EngineExportCommand>("export")
            .WithDescription("Export pages (png/pdf)")
            .WithExample(new[] { "engine", "export", "--file", "path/to/project.json", "--out", "out", "--format", "pdf" });
    });

    config.AddBranch("images", images =>
    {
        images.SetDescription("Import images into project workspace");
        images.AddCommand<ImagesAddFrontCommand>("add-front")
            .WithDescription("Import front images into a project")
            .WithExample(new[] { "images", "add-front", "skolni-pexeso", "--from", "assets/front", "--recursive" });
        images.AddCommand<ImagesSetBackCommand>("set-back")
            .WithDescription("Set the back image for a project")
            .WithExample(new[] { "images", "set-back", "skolni-pexeso", "--file", "assets/back.png" });
    });

    config.AddBranch("project", project =>
    {
        project.SetDescription("Project build commands");
        project.AddCommand<ProjectBuildCommand>("build")
            .WithDescription("Build engine project.json from workspace images")
            .WithExample(new[] { "project", "build", "skolni-pexeso", "--pairs", "12", "--dpi", "300" });
    });

    config.AddBranch("presets", presets =>
    {
        presets.SetDescription("Preset listing commands");
        presets.AddCommand<PresetsListCommand>("list")
            .WithDescription("List available layout presets")
            .WithExample(new[] { "presets", "list" });
    });

    config.AddCommand<MakeCommand>("make")
        .WithDescription("One-shot project generation and export")
        .WithExample(new[]
        {
            "make",
            "skolni-pexeso",
            "--name",
            "Pexeso do prvouky",
            "--front-dir",
            "assets/front",
            "--pairs",
            "12",
            "--format",
            "pdf",
        });
});

return await RunAppAsync(args).ConfigureAwait(false);

async Task<int> RunAppAsync(string[] args)
{
    try
    {
        return await app.RunAsync(args).ConfigureAwait(false);
    }
    catch (CommandException ex)
    {
        AnsiConsole.MarkupLine($"[red]{ex.Message.EscapeMarkup()}[/]");
        return 2;
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Unexpected error: {ex.Message.EscapeMarkup()}[/]");
        return 1;
    }
}
