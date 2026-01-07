namespace PexMaker.Engine.Domain;

public sealed record LayoutPreset(
    string Id,
    string Name,
    PageFormat Format,
    PageOrientation Orientation,
    GridSpec Grid,
    Mm Margin,
    Mm Gutter,
    AutoFitMode AutoFitMode,
    bool PreferSquareCards,
    GridAlignment Alignment,
    DuplexMode DuplexMode);
