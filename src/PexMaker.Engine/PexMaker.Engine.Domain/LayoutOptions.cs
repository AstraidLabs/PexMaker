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

    [Obsolete("Use DuplexMode instead.")]
    public bool MirrorBackside { get; init; } = true;

    public DuplexMode DuplexMode { get; init; } = EngineDefaults.DefaultDuplexMode;

    public GridAlignment Alignment { get; init; } = EngineDefaults.DefaultGridAlignment;

    public AutoFitMode AutoFitMode { get; init; } = EngineDefaults.DefaultAutoFitMode;

    public GridSpec? Grid { get; init; }

    public bool PreferSquareCards { get; init; } = false;

    public Mm Bleed { get; init; } = Mm.Zero;

    public bool CutMarks { get; init; } = false;

    public Mm CutMarkLength { get; init; } = EngineDefaults.DefaultCutMarkLength;

    public Mm CutMarkThickness { get; init; } = EngineDefaults.DefaultCutMarkThickness;

    public Mm CutMarkOffset { get; init; } = EngineDefaults.DefaultCutMarkOffset;
}
