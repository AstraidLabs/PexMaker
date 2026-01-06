using FluentAssertions;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Application;
using PexMaker.Engine.Domain;
using PexMaker.Engine.Infrastructure;
using SkiaSharp;

namespace PexMaker.Engine.Tests;

public class EngineTests
{
    [Fact]
    public async Task Deck_is_deterministic_and_contains_pairs()
    {
        var engine = PexMakerEngineFactory.CreateDefault();
        var project = CreateProject(pairCount: 3);

        var deck1 = await engine.BuildDeckAsync(project, seed: 42, CancellationToken.None);
        var deck2 = await engine.BuildDeckAsync(project, seed: 42, CancellationToken.None);

        deck1.Cards.Should().Equal(deck2.Cards);
        deck1.Cards.GroupBy(c => c.Path).All(g => g.Count() == 2).Should().BeTrue();
    }

    [Fact]
    public async Task Layout_grid_matches_expected_dimensions()
    {
        var project = CreateProject(pairCount: 4, configure: p =>
        {
            p.Layout = p.Layout with
            {
                MarginLeft = new Mm(10),
                MarginTop = new Mm(10),
                MarginRight = new Mm(10),
                MarginBottom = new Mm(10),
                CardWidth = new Mm(60),
                CardHeight = new Mm(80),
                Gutter = new Mm(5),
            };
        });

        var engine = PexMakerEngineFactory.CreateDefault();
        var validation = await engine.ValidateAsync(project, CancellationToken.None);
        validation.IsValid.Should().BeTrue();

        var layout = await engine.ComputeLayoutAsync(project, CancellationToken.None);
        layout.Grid.Columns.Should().Be(3);
        layout.Grid.Rows.Should().Be(3);
        layout.Grid.PerPage.Should().Be(9);
    }

    [Fact]
    public async Task Invalid_layout_reports_errors()
    {
        var project = CreateProject(pairCount: 1, configure: p =>
        {
            p.Layout = p.Layout with
            {
                MarginLeft = new Mm(5000),
                MarginTop = new Mm(5000),
                MarginRight = new Mm(5000),
                MarginBottom = new Mm(5000),
            };
        });

        var engine = PexMakerEngineFactory.CreateDefault();
        var validation = await engine.ValidateAsync(project, CancellationToken.None);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.Code == EngineErrorCode.LayoutDoesNotFit);
    }

    [Fact]
    public void Units_convert_mm_to_px_and_back()
    {
        var pixels = Units.MmToPx(new Mm(25.4), new Dpi(300));
        pixels.Should().Be(300);

        var mm = Units.PxToMm(pixels, new Dpi(300));
        mm.Should().BeApproximately(25.4, 0.1);
    }

    [Fact]
    public void Naming_formats_file_names()
    {
        Naming.PageFileName("front", 1, ExportImageFormat.Png).Should().Be("front_001.png");
        Naming.SafeFileName("a<b>c").Should().Be("a_b_c");
    }

    [Fact]
    public async Task Export_pipeline_writes_expected_files()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var outputDir = Path.Combine(tempRoot, "out");
        Directory.CreateDirectory(outputDir);

        try
        {
            var front1 = CreateImage(Path.Combine(tempRoot, "f1.png"), 200, 200, SKColors.Red);
            var front2 = CreateImage(Path.Combine(tempRoot, "f2.png"), 200, 200, SKColors.Blue);
            var back = CreateImage(Path.Combine(tempRoot, "back.png"), 200, 200, SKColors.Gray);

            var project = new PexProject
            {
                PairCount = 2,
                FrontImages = new List<ImageRef>
                {
                    new() { Path = front1 },
                    new() { Path = front2 },
                },
                BackImage = new ImageRef { Path = back },
                Dpi = new Dpi(300),
            };

            var engine = PexMakerEngineFactory.CreateDefault();
            var result = await engine.ExportAsync(project, new ExportRequest
            {
                OutputDirectory = outputDir,
                Format = ExportImageFormat.Png,
            }, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Files.Should().HaveCount(2);
            result.Files.All(File.Exists).Should().BeTrue();

            using var bitmap = SKBitmap.Decode(result.Files.First());
            bitmap.Should().NotBeNull();
            bitmap!.Width.Should().BeGreaterThan(0);
            bitmap!.Height.Should().BeGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string CreateImage(string path, int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        data.SaveTo(stream);
        return path;
    }

    private static PexProject CreateProject(int pairCount, Action<PexProject>? configure = null)
    {
        var project = new PexProject
        {
            PairCount = pairCount,
            FrontImages = Enumerable.Range(0, pairCount).Select(i => new ImageRef { Path = $"front_{i}.png" }).ToList(),
            BackImage = new ImageRef { Path = "back.png" },
            Dpi = new Dpi(300),
        };

        configure?.Invoke(project);
        return project;
    }
}
