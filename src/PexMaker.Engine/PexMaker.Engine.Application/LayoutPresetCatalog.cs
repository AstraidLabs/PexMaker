using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed class LayoutPresetCatalog
{
    private static readonly IReadOnlyList<LayoutPreset> Presets =
    [
        new LayoutPreset(
            "a4-portrait-3x4",
            "A4 Portrait 3x4",
            PageFormat.A4,
            PageOrientation.Portrait,
            new GridSpec(3, 4),
            EngineDefaults.DefaultMargin,
            EngineDefaults.DefaultGutter,
            AutoFitMode.PreserveMarginsAndGutter,
            false,
            GridAlignment.Center,
            DuplexMode.FlipLongEdge),
        new LayoutPreset(
            "a4-portrait-4x5",
            "A4 Portrait 4x5",
            PageFormat.A4,
            PageOrientation.Portrait,
            new GridSpec(4, 5),
            EngineDefaults.DefaultMargin,
            EngineDefaults.DefaultGutter,
            AutoFitMode.PreserveMarginsAndGutter,
            false,
            GridAlignment.Center,
            DuplexMode.FlipLongEdge),
        new LayoutPreset(
            "a4-portrait-5x6",
            "A4 Portrait 5x6",
            PageFormat.A4,
            PageOrientation.Portrait,
            new GridSpec(5, 6),
            EngineDefaults.DefaultMargin,
            EngineDefaults.DefaultGutter,
            AutoFitMode.PreserveMarginsAndGutter,
            false,
            GridAlignment.Center,
            DuplexMode.FlipLongEdge),
        new LayoutPreset(
            "a4-landscape-3x4",
            "A4 Landscape 3x4",
            PageFormat.A4,
            PageOrientation.Landscape,
            new GridSpec(3, 4),
            EngineDefaults.DefaultMargin,
            EngineDefaults.DefaultGutter,
            AutoFitMode.PreserveMarginsAndGutter,
            false,
            GridAlignment.Center,
            DuplexMode.FlipShortEdge),
        new LayoutPreset(
            "a4-landscape-4x5",
            "A4 Landscape 4x5",
            PageFormat.A4,
            PageOrientation.Landscape,
            new GridSpec(4, 5),
            EngineDefaults.DefaultMargin,
            EngineDefaults.DefaultGutter,
            AutoFitMode.PreserveMarginsAndGutter,
            false,
            GridAlignment.Center,
            DuplexMode.FlipShortEdge),
        new LayoutPreset(
            "a4-landscape-5x6",
            "A4 Landscape 5x6",
            PageFormat.A4,
            PageOrientation.Landscape,
            new GridSpec(5, 6),
            EngineDefaults.DefaultMargin,
            EngineDefaults.DefaultGutter,
            AutoFitMode.PreserveMarginsAndGutter,
            false,
            GridAlignment.Center,
            DuplexMode.FlipShortEdge),
    ];

    public IReadOnlyList<LayoutPreset> GetAll() => Presets;

    public bool TryGetById(string id, out LayoutPreset preset)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            preset = default!;
            return false;
        }

        var resolved = Presets.FirstOrDefault(preset => string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase));
        if (resolved is null)
        {
            preset = default!;
            return false;
        }

        preset = resolved;
        return true;
    }
}
