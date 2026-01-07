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
        var (project, tempRoot) = CreateProject(pairCount: 3);

        try
        {
            var deck1Result = await engine.BuildDeckAsync(project, seed: 42, CancellationToken.None);
            var deck2Result = await engine.BuildDeckAsync(project, seed: 42, CancellationToken.None);
            deck1Result.IsSuccess.Should().BeTrue();
            deck2Result.IsSuccess.Should().BeTrue();
            var deck1 = deck1Result.Value!;
            var deck2 = deck2Result.Value!;

            deck1.Cards.Should().Equal(deck2.Cards);
            deck1.Cards.GroupBy(c => c.Path).All(g => g.Count() == 2).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Layout_grid_matches_expected_dimensions()
    {
        var (project, tempRoot) = CreateProject(pairCount: 4, configure: p =>
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

        try
        {
            var engine = PexMakerEngineFactory.CreateDefault();
            var validation = await engine.ValidateAsync(project, CancellationToken.None);
            validation.IsValid.Should().BeTrue();

            var layoutResult = await engine.ComputeLayoutAsync(project, CancellationToken.None);
            layoutResult.IsSuccess.Should().BeTrue();
            var layout = layoutResult.Value!;
            layout.Grid.Columns.Should().Be(3);
            layout.Grid.Rows.Should().Be(3);
            layout.Grid.PerPage.Should().Be(9);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Invalid_layout_reports_errors()
    {
        var (project, tempRoot) = CreateProject(pairCount: 1, configure: p =>
        {
            p.Layout = p.Layout with
            {
                MarginLeft = new Mm(5000),
                MarginTop = new Mm(5000),
                MarginRight = new Mm(5000),
                MarginBottom = new Mm(5000),
            };
        });

        try
        {
            var engine = PexMakerEngineFactory.CreateDefault();
            var validation = await engine.ValidateAsync(project, CancellationToken.None);

            validation.IsValid.Should().BeFalse();
            validation.Errors.Should().Contain(e => e.Code == EngineErrorCode.LayoutDoesNotFit);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
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
                Format = "png",
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

    private static (PexProject Project, string TempRoot) CreateProject(int pairCount, Action<PexProject>? configure = null)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var frontImages = new List<ImageRef>();
        for (var i = 0; i < pairCount; i++)
        {
            var path = Path.Combine(tempRoot, $"front_{i}.png");
            File.WriteAllBytes(path, Array.Empty<byte>());
            frontImages.Add(new ImageRef { Path = path });
        }

        var backPath = Path.Combine(tempRoot, "back.png");
        File.WriteAllBytes(backPath, Array.Empty<byte>());

        var project = new PexProject
        {
            PairCount = pairCount,
            FrontImages = frontImages,
            BackImage = new ImageRef { Path = backPath },
            Dpi = new Dpi(300),
        };

        configure?.Invoke(project);
        return (project, tempRoot);
    }
}
