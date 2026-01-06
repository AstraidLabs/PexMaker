using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public static class Units
{
    private const double MillimetersPerInch = 25.4;

    public static int MmToPx(Mm mm, Dpi dpi) => (int)Math.Round(mm.Value / MillimetersPerInch * dpi.Value);

    public static double PxToMm(int pixels, Dpi dpi) => pixels / (double)dpi.Value * MillimetersPerInch;

    public static double MmToPoints(Mm mm) => mm.Value * 72d / MillimetersPerInch;
}
