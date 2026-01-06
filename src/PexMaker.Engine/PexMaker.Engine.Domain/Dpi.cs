namespace PexMaker.Engine.Domain;

/// <summary>
/// Represents dots-per-inch resolution.
/// </summary>
public readonly record struct Dpi(int Value)
{
    public static Dpi From(int value) => new(value);

    public override string ToString() => $"{Value} dpi";

    public static implicit operator int(Dpi dpi) => dpi.Value;
}
