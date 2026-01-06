namespace PexMaker.Engine.Domain;

/// <summary>
/// Reference to an image on disk.
/// </summary>
public sealed record ImageRef
{
    public required string Path { get; init; }

    public FitMode FitMode { get; init; } = EngineDefaults.DefaultFitMode;

    public double AnchorX { get; init; } = 0.5;

    public double AnchorY { get; init; } = 0.5;

    public double RotationDegrees { get; init; } = 0;
}
