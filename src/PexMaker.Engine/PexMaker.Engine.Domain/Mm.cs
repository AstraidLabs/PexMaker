namespace PexMaker.Engine.Domain;

/// <summary>
/// Represents a millimeter measurement.
/// </summary>
public readonly record struct Mm(double Value)
{
    public static Mm Zero => new(0);

    public bool IsFinite => MathEx.IsFinite(Value);

    public static Mm From(double value) => new(value);

    public override string ToString() => $"{Value:0.###} mm";

    public static implicit operator double(Mm mm) => mm.Value;
}
