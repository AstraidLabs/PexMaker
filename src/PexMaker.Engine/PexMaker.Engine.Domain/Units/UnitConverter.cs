using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Domain.Units;

public enum Unit
{
    Millimeter,
    Inch,
    Point,
    Pixel,
}

public enum PxRounding
{
    Round,
    Floor,
    Ceil,
}

public readonly record struct ResolutionDpi(int Value)
{
    public static ResolutionDpi Create(int dpi)
    {
        Guard.InRange(dpi, EngineLimits.MinDpi, EngineLimits.MaxDpi, nameof(dpi));
        return new ResolutionDpi(dpi);
    }
}

public static class UnitConverter
{
    public const decimal MillimetersPerInch = 25.4m;
    public const decimal PointsPerInch = 72m;

    public static decimal Convert(decimal value, Unit from, Unit to, ResolutionDpi? dpi = null)
    {
        if (from == to)
        {
            return value;
        }

        if ((from == Unit.Pixel || to == Unit.Pixel) && dpi is null)
        {
            throw new ArgumentNullException(nameof(dpi));
        }

        var inches = ToInches(value, from, dpi);
        return FromInches(inches, to, dpi);
    }

    public static int ToPixels(decimal value, Unit from, ResolutionDpi dpi, PxRounding rounding)
    {
        var pixels = Convert(value, from, Unit.Pixel, dpi);
        return rounding switch
        {
            PxRounding.Round => (int)Math.Round(pixels),
            PxRounding.Floor => (int)Math.Floor(pixels),
            PxRounding.Ceil => (int)Math.Ceiling(pixels),
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, "Unsupported rounding mode."),
        };
    }

    public static decimal FromPixels(int px, Unit to, ResolutionDpi dpi)
    {
        return Convert(px, Unit.Pixel, to, dpi);
    }

    private static decimal ToInches(decimal value, Unit from, ResolutionDpi? dpi)
    {
        return from switch
        {
            Unit.Millimeter => value / MillimetersPerInch,
            Unit.Inch => value,
            Unit.Point => value / PointsPerInch,
            Unit.Pixel => value / (dpi?.Value ?? throw new ArgumentNullException(nameof(dpi))),
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, "Unsupported unit."),
        };
    }

    private static decimal FromInches(decimal inches, Unit to, ResolutionDpi? dpi)
    {
        return to switch
        {
            Unit.Millimeter => inches * MillimetersPerInch,
            Unit.Inch => inches,
            Unit.Point => inches * PointsPerInch,
            Unit.Pixel => inches * (dpi?.Value ?? throw new ArgumentNullException(nameof(dpi))),
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, "Unsupported unit."),
        };
    }
}
