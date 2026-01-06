namespace PexMaker.Engine.Domain;

public static class MathEx
{
    public static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    public static double Clamp01(double value) => Clamp(value, 0d, 1d);

    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    public static bool NearlyEquals(double a, double b, double tolerance = 1e-6) => Math.Abs(a - b) <= tolerance;
}
