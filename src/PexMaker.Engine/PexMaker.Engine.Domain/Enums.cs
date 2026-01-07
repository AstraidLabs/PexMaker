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

public enum RegistrationMarkPlacement
{
    CornersOutsideGrid,
}

public enum EngineErrorCode
{
    Unknown = 0,
    InvalidPairCount,
    InvalidFrontImages,
    InsufficientFrontImages,
    InvalidImagePath,
    ImageNotFound,
    MissingBackImage,
    InvalidDpi,
    InvalidMeasurement,
    InvalidPageFormat,
    InvalidPageOrientation,
    InvalidDuplexMode,
    InvalidAlignment,
    InvalidAutoFitMode,
    InvalidGrid,
    InvalidCardSize,
    InvalidBleed,
    InvalidCutMarks,
    InvalidSafeArea,
    InvalidRegistrationMarks,
    InvalidDuplexOffset,
    LayoutDoesNotFit,
    InvalidPageSize,
    ImageTooLarge,
    ExceedsPageLimit,
    ExceedsOutputLimit,
    OutputDirectoryMissing,
    InvalidOutputDirectory,
    InvalidNamingPrefix,
    NamingCollision,
    InvalidExportSelection,
    InvalidParallelism,
    InvalidBufferLimit,
    InvalidCacheLimit,
    InvalidWorkingSetLimit,
    ProjectLoadFailed,
    WorkingSetTooLarge,
    UnsupportedFormat,
    OutputFileExists,
    ExportCanceled,
}
