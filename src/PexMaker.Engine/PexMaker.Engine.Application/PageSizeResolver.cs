using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal static class PageSizeResolver
{
    public static (double WidthMm, double HeightMm) ResolvePageSize(LayoutOptions layout)
    {
        var (w, h) = layout.Format switch
        {
            PageFormat.A4 => (EngineDefaults.A4Width.Value, EngineDefaults.A4Height.Value),
            PageFormat.A3 => (EngineDefaults.A3Width.Value, EngineDefaults.A3Height.Value),
            PageFormat.Letter => (EngineDefaults.LetterWidth.Value, EngineDefaults.LetterHeight.Value),
            _ => (EngineDefaults.A4Width.Value, EngineDefaults.A4Height.Value),
        };

        return layout.Orientation == PageOrientation.Portrait ? (w, h) : (h, w);
    }
}
