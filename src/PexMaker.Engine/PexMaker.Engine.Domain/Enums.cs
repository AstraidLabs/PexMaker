namespace PexMaker.Engine.Domain;

public enum PageFormat
{
    A4,
    A3,
    Letter,
}

public enum PageOrientation
{
    Portrait,
    Landscape,
}

public enum FitMode
{
    Cover,
    Contain,
}

public enum DuplexMode
{
    None,
    MirrorColumns,
    MirrorRows,
    MirrorBoth,
    FlipLongEdge,
    FlipShortEdge,
}

public enum GridAlignment
{
    TopLeft,
    Center,
}

public enum AutoFitMode
{
    None,
    PreserveMarginsAndGutter,
}

public enum SheetSide
{
    Front,
    Back,
}

public enum EngineErrorCode
{
    Unknown = 0,
    InvalidPairCount,
    InsufficientFrontImages,
    MissingBackImage,
    InvalidDpi,
    InvalidMeasurement,
    LayoutDoesNotFit,
    ExceedsPageLimit,
    ExceedsOutputLimit,
    WorkingSetTooLarge,
    UnsupportedFormat,
    ExportCanceled,
}
