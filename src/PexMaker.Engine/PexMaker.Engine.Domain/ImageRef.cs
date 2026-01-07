namespace PexMaker.Engine.Domain;

/// <summary>
/// Reference to an image on disk.
/// </summary>
public sealed record ImageRef
{
    /// <summary>
    /// Absolute or relative path to the image file.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Fit mode for image placement.
    /// </summary>
    public FitMode FitMode { get; init; } = EngineDefaults.DefaultFitMode;

    /// <summary>
    /// Horizontal anchor (0..1).
    /// </summary>
    public double AnchorX { get; init; } = 0.5;

    /// <summary>
    /// Vertical anchor (0..1).
    /// </summary>
    public double AnchorY { get; init; } = 0.5;

    /// <summary>
    /// Rotation in degrees clockwise around the card center.
    /// </summary>
    public double RotationDegrees { get; init; } = 0;
}
