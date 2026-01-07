namespace PexMaker.Engine.Contracts;

public sealed record LayoutOptionsDto
{
    public string? PageFormat { get; init; }

    public bool Landscape { get; init; }

    public MarginDto? Margins { get; init; }

    public double GutterMm { get; init; }

    public SizeDto? CardSizeMm { get; init; }

    public double CornerRadiusMm { get; init; }

    public bool MirrorBackside { get; init; }

    public string? DuplexMode { get; init; }

    public string? Alignment { get; init; }

    public string? AutoFitMode { get; init; }

    public int? GridColumns { get; init; }

    public int? GridRows { get; init; }

    public bool PreferSquareCards { get; init; }

    public bool DrawBorder { get; init; }

    public double BorderThicknessMm { get; init; }

    public double BleedMm { get; init; }

    public bool DrawCutMarks { get; init; }

    public double CutMarkLengthMm { get; init; }

    public double CutMarkThicknessMm { get; init; }

    public double CutMarkOffsetMm { get; init; }
}
