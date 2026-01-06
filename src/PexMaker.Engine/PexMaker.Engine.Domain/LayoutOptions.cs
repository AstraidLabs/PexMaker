namespace PexMaker.Engine.Domain;

/// <summary>
/// Layout options for page composition.
/// </summary>
public sealed record LayoutOptions
{
    public PageFormat Format { get; init; } = EngineDefaults.DefaultPageFormat;

    public PageOrientation Orientation { get; init; } = EngineDefaults.DefaultOrientation;

    public Mm MarginLeft { get; init; } = EngineDefaults.DefaultMargin;

    public Mm MarginTop { get; init; } = EngineDefaults.DefaultMargin;

    public Mm MarginRight { get; init; } = EngineDefaults.DefaultMargin;

    public Mm MarginBottom { get; init; } = EngineDefaults.DefaultMargin;

    public Mm Gutter { get; init; } = EngineDefaults.DefaultGutter;

    public Mm CardWidth { get; init; } = EngineDefaults.DefaultCardWidth;

    public Mm CardHeight { get; init; } = EngineDefaults.DefaultCardHeight;

    public Mm CornerRadius { get; init; } = EngineDefaults.DefaultCornerRadius;

    public bool BorderEnabled { get; init; } = true;

    public Mm BorderThickness { get; init; } = EngineDefaults.DefaultBorderThickness;

    public bool MirrorBackside { get; init; } = true;

    public Mm Bleed { get; init; } = Mm.Zero;

    public bool CutMarks { get; init; } = false;
}
