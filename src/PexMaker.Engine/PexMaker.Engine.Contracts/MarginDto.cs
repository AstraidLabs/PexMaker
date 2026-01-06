namespace PexMaker.Engine.Contracts;

public sealed record MarginDto
{
    public double LeftMm { get; init; }

    public double TopMm { get; init; }

    public double RightMm { get; init; }

    public double BottomMm { get; init; }
}
