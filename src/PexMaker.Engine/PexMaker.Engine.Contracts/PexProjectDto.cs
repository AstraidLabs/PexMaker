namespace PexMaker.Engine.Contracts;

public sealed record PexProjectDto
{
    public int SchemaVersion { get; init; }

    public string? Id { get; init; }

    public string? Name { get; init; }

    public int PairCount { get; init; }

    public List<ImageRefDto>? FrontImages { get; init; }

    public ImageRefDto? BackImage { get; init; }

    public LayoutOptionsDto? Layout { get; init; }

    public int Dpi { get; init; }
}
