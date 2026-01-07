namespace PexMaker.Engine.Application;

public sealed record LayoutPreview(
    string? PresetId,
    int Columns,
    int Rows,
    int PerPage,
    int Pages,
    double CardWidthMm,
    double CardHeightMm,
    double GridWidthMm,
    double GridHeightMm,
    double OriginXmm,
    double OriginYmm,
    double UsableWidthMm,
    double UsableHeightMm,
    double PageWidthMm,
    double PageHeightMm,
    int PageWidthPx,
    int PageHeightPx,
    int CardWidthPx,
    int CardHeightPx);
