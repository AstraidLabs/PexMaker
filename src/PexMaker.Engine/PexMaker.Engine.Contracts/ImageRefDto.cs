namespace PexMaker.Engine.Contracts;

public sealed record ImageRefDto
{
    public string? Path { get; init; }

    public string? FitMode { get; init; }

    public float CropX { get; init; }

    public float CropY { get; init; }

    public float? RotationDeg { get; init; }
}
