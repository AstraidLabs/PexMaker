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

    public bool DrawBorder { get; init; }

    public double BorderThicknessMm { get; init; }

    public double BleedMm { get; init; }

    public bool DrawCutMarks { get; init; }
}
