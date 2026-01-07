namespace PexMaker.Engine.Domain;

/// <summary>
/// Represents a memory card printing project.
/// </summary>
public sealed class PexProject
{
    /// <summary>
    /// Number of card pairs to generate. Must be within engine limits.
    /// </summary>
    public int PairCount { get; init; } = 1;

    /// <summary>
    /// Front image references. Must contain at least <see cref="PairCount"/> entries.
    /// </summary>
    public List<ImageRef> FrontImages { get; init; } = [];

    /// <summary>
    /// Back image reference (required).
    /// </summary>
    public ImageRef? BackImage { get; init; }

    /// <summary>
    /// Layout options for page composition.
    /// </summary>
    public LayoutOptions Layout { get; init; } = new();

    /// <summary>
    /// Output resolution in DPI.
    /// </summary>
    public Dpi Dpi { get; init; } = new(EngineDefaults.DefaultDpi);
}
