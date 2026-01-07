namespace PexMaker.Engine.Domain;

/// <summary>
/// Layout options for page composition.
/// </summary>
public sealed record LayoutOptions
{
    /// <summary>
    /// Page format (A4/A3/Letter).
    /// </summary>
    public PageFormat Format { get; init; } = EngineDefaults.DefaultPageFormat;

    /// <summary>
    /// Page orientation.
    /// </summary>
    public PageOrientation Orientation { get; init; } = EngineDefaults.DefaultOrientation;

    /// <summary>
    /// Left margin in millimeters.
    /// </summary>
    public Mm MarginLeft { get; init; } = EngineDefaults.DefaultMargin;

    /// <summary>
    /// Top margin in millimeters.
    /// </summary>
    public Mm MarginTop { get; init; } = EngineDefaults.DefaultMargin;

    /// <summary>
    /// Right margin in millimeters.
    /// </summary>
    public Mm MarginRight { get; init; } = EngineDefaults.DefaultMargin;

    /// <summary>
    /// Bottom margin in millimeters.
    /// </summary>
    public Mm MarginBottom { get; init; } = EngineDefaults.DefaultMargin;

    /// <summary>
    /// Gutter between cards.
    /// </summary>
    public Mm Gutter { get; init; } = EngineDefaults.DefaultGutter;

    /// <summary>
    /// Card width in millimeters.
    /// </summary>
    public Mm CardWidth { get; init; } = EngineDefaults.DefaultCardWidth;

    /// <summary>
    /// Card height in millimeters.
    /// </summary>
    public Mm CardHeight { get; init; } = EngineDefaults.DefaultCardHeight;

    /// <summary>
    /// Corner radius in millimeters.
    /// </summary>
    public Mm CornerRadius { get; init; } = EngineDefaults.DefaultCornerRadius;

    /// <summary>
    /// Enables card border rendering.
    /// </summary>
    public bool BorderEnabled { get; init; } = true;

    /// <summary>
    /// Border thickness in millimeters.
    /// </summary>
    public Mm BorderThickness { get; init; } = EngineDefaults.DefaultBorderThickness;

    [Obsolete("Use DuplexMode instead.")]
    public bool MirrorBackside { get; init; } = true;

    public DuplexMode DuplexMode { get; init; } = EngineDefaults.DefaultDuplexMode;

    /// <summary>
    /// Grid alignment on the page.
    /// </summary>
    public GridAlignment Alignment { get; init; } = EngineDefaults.DefaultGridAlignment;

    /// <summary>
    /// Auto-fit mode for grid-driven layouts.
    /// </summary>
    public AutoFitMode AutoFitMode { get; init; } = EngineDefaults.DefaultAutoFitMode;

    public GridSpec? Grid { get; init; }

    public bool PreferSquareCards { get; init; } = false;

    /// <summary>
    /// Bleed in millimeters. Bleed expands the rendered image beyond the trim size.
    /// </summary>
    public Mm Bleed { get; init; } = Mm.Zero;

    /// <summary>
    /// Enables cut marks.
    /// </summary>
    public bool CutMarks { get; init; } = false;

    /// <summary>
    /// Draw cut marks for every card instead of only the overall grid.
    /// </summary>
    public bool CutMarksPerCard { get; init; } = EngineDefaults.DefaultCutMarksPerCard;

    /// <summary>
    /// Cut mark length in millimeters.
    /// </summary>
    public Mm CutMarkLength { get; init; } = EngineDefaults.DefaultCutMarkLength;

    /// <summary>
    /// Cut mark thickness in millimeters.
    /// </summary>
    public Mm CutMarkThickness { get; init; } = EngineDefaults.DefaultCutMarkThickness;

    /// <summary>
    /// Cut mark offset from the card edge in millimeters.
    /// </summary>
    public Mm CutMarkOffset { get; init; } = EngineDefaults.DefaultCutMarkOffset;

    /// <summary>
    /// Safe area inset from the trim edge in millimeters.
    /// </summary>
    public Mm SafeArea { get; init; } = Mm.Zero;

    /// <summary>
    /// Show a safe area overlay in exports/previews.
    /// </summary>
    public bool ShowSafeAreaOverlay { get; init; } = false;

    /// <summary>
    /// Safe area overlay stroke thickness in millimeters.
    /// </summary>
    public Mm SafeAreaOverlayThickness { get; init; } = EngineDefaults.DefaultSafeAreaOverlayThickness;

    /// <summary>
    /// Enables registration marks for duplex alignment.
    /// </summary>
    public bool IncludeRegistrationMarks { get; init; } = false;

    /// <summary>
    /// Registration mark size in millimeters.
    /// </summary>
    public Mm RegistrationMarkSize { get; init; } = EngineDefaults.DefaultRegistrationMarkSize;

    /// <summary>
    /// Registration mark line thickness in millimeters.
    /// </summary>
    public Mm RegistrationMarkThickness { get; init; } = EngineDefaults.DefaultRegistrationMarkThickness;

    /// <summary>
    /// Registration mark offset from the grid in millimeters.
    /// </summary>
    public Mm RegistrationMarkOffset { get; init; } = EngineDefaults.DefaultRegistrationMarkOffset;

    /// <summary>
    /// Registration mark placement strategy.
    /// </summary>
    public RegistrationMarkPlacement RegistrationMarkPlacement { get; init; } = EngineDefaults.DefaultRegistrationMarkPlacement;

    /// <summary>
    /// Duplex offset (X) in millimeters applied to the back side only.
    /// </summary>
    public Mm DuplexOffsetX { get; init; } = Mm.Zero;

    /// <summary>
    /// Duplex offset (Y) in millimeters applied to the back side only.
    /// </summary>
    public Mm DuplexOffsetY { get; init; } = Mm.Zero;
}
