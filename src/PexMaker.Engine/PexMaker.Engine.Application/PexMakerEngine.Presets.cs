using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    private readonly LayoutPresetCatalog _presetCatalog = new();

    public IReadOnlyList<LayoutPreset> GetPresets() => _presetCatalog.GetAll();

    public bool TryGetPresetById(string id, out LayoutPreset preset) => _presetCatalog.TryGetById(id, out preset);

    public PexProject ApplyPreset(PexProject project, string presetId)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!_presetCatalog.TryGetById(presetId, out var preset))
        {
            throw new ArgumentException($"Unknown layout preset '{presetId}'.", nameof(presetId));
        }

        return ApplyPreset(project, preset);
    }

    public LayoutPreview PreviewLayout(PexProject project, string? presetId = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        LayoutPreset? preset = null;
        var layout = project.Layout;

        if (!string.IsNullOrWhiteSpace(presetId))
        {
            if (!_presetCatalog.TryGetById(presetId, out var resolved))
            {
                throw new ArgumentException($"Unknown layout preset '{presetId}'.", nameof(presetId));
            }

            preset = resolved;
            layout = ApplyPreset(layout, resolved);
        }

        var metrics = LayoutCalculator.Calculate(layout);
        var pageWidthPx = Units.MmToPx(new Mm(metrics.PageWidthMm), project.Dpi);
        var pageHeightPx = Units.MmToPx(new Mm(metrics.PageHeightMm), project.Dpi);
        var cardWidthPx = Units.MmToPx(new Mm(metrics.CardWidthMm), project.Dpi);
        var cardHeightPx = Units.MmToPx(new Mm(metrics.CardHeightMm), project.Dpi);
        var totalCards = Math.Max(0, project.PairCount) * 2;
        var pages = metrics.PerPage > 0 ? (int)Math.Ceiling(totalCards / (double)metrics.PerPage) : 0;

        return new LayoutPreview(
            preset?.Id,
            metrics.Columns,
            metrics.Rows,
            metrics.PerPage,
            pages,
            metrics.CardWidthMm,
            metrics.CardHeightMm,
            metrics.GridWidthMm,
            metrics.GridHeightMm,
            metrics.OriginXmm,
            metrics.OriginYmm,
            metrics.UsableWidthMm,
            metrics.UsableHeightMm,
            metrics.PageWidthMm,
            metrics.PageHeightMm,
            pageWidthPx,
            pageHeightPx,
            cardWidthPx,
            cardHeightPx);
    }

    private static PexProject ApplyPreset(PexProject project, LayoutPreset preset)
    {
        var layout = ApplyPreset(project.Layout, preset);
        return new PexProject
        {
            PairCount = project.PairCount,
            FrontImages = project.FrontImages,
            BackImage = project.BackImage,
            Layout = layout,
            Dpi = project.Dpi,
        };
    }

    private static LayoutOptions ApplyPreset(LayoutOptions layout, LayoutPreset preset)
    {
        return layout with
        {
            Format = preset.Format,
            Orientation = preset.Orientation,
            MarginLeft = preset.Margin,
            MarginTop = preset.Margin,
            MarginRight = preset.Margin,
            MarginBottom = preset.Margin,
            Gutter = preset.Gutter,
            AutoFitMode = preset.AutoFitMode,
            Grid = preset.Grid,
            PreferSquareCards = preset.PreferSquareCards,
            Alignment = preset.Alignment,
            DuplexMode = preset.DuplexMode,
        };
    }
}
