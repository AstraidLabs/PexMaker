namespace PexMaker.Engine.Domain;

/// <summary>
/// Represents a memory card printing project.
/// </summary>
public sealed class PexProject
{
    public int PairCount { get; init; } = 1;

    public List<ImageRef> FrontImages { get; init; } = [];

    public ImageRef? BackImage { get; init; }

    public LayoutOptions Layout { get; init; } = new();

    public Dpi Dpi { get; init; } = new(EngineDefaults.DefaultDpi);
}
