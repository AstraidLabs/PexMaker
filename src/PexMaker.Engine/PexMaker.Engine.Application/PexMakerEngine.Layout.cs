using PexMaker.Engine.Domain;
using PexMaker.Engine.Domain.Units;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    /// <summary>
    /// Computes a layout plan after validating and normalizing project inputs.
    /// </summary>
    public Task<EngineOperationResult<LayoutPlan>> ComputeLayoutAsync(PexProject project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateProject(project, out var normalizedProject);
        if (!validation.IsValid)
        {
            return Task.FromResult(new EngineOperationResult<LayoutPlan>(null, validation));
        }

        var deck = BuildSequentialDeck(normalizedProject);
        var metrics = LayoutCalculator.Calculate(normalizedProject.Layout);
        var plan = BuildLayoutPlan(normalizedProject, deck, metrics);
        return Task.FromResult(new EngineOperationResult<LayoutPlan>(plan, validation));
    }

    private static Deck BuildSequentialDeck(PexProject project)
    {
        if (project.BackImage is null)
        {
            throw new InvalidOperationException("Back image is required.");
        }

        var cards = new List<ImageRef>(project.PairCount * 2);
        for (var i = 0; i < project.PairCount; i++)
        {
            cards.Add(project.FrontImages[i]);
            cards.Add(project.FrontImages[i]);
        }

        return new Deck(cards, project.BackImage);
    }

    private LayoutPlan BuildLayoutPlan(PexProject project, Deck deck, LayoutMetrics metrics)
    {
        var layout = project.Layout;
        var resolution = ResolutionDpi.Create(project.Dpi.Value);
        var pageWidthPx = UnitConverter.ToPixels((decimal)metrics.PageWidthMm, Unit.Millimeter, resolution, PxRounding.Round);
        var pageHeightPx = UnitConverter.ToPixels((decimal)metrics.PageHeightMm, Unit.Millimeter, resolution, PxRounding.Round);

        var grid = new LayoutGrid(metrics.Columns, metrics.Rows, metrics.PerPage);
        if (grid.PerPage < 1 || metrics.CardWidthMm <= 0 || metrics.CardHeightMm <= 0)
        {
            throw new InvalidOperationException("Layout cannot be computed because no cards fit on a page.");
        }

        var pages = new List<LayoutPage>();
        var cardWidthPx = UnitConverter.ToPixels((decimal)metrics.CardWidthMm, Unit.Millimeter, resolution, PxRounding.Round);
        var cardHeightPx = UnitConverter.ToPixels((decimal)metrics.CardHeightMm, Unit.Millimeter, resolution, PxRounding.Round);
        var perPage = grid.PerPage;
        var totalPages = (int)Math.Ceiling(deck.CardCount / (double)perPage);
        var normalizedDuplex = NormalizeDuplex(layout);

        for (var pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            var frontPlacements = new List<CardPlacementPlan>();
            var backPlacements = new List<CardPlacementPlan>();

            for (var slot = 0; slot < perPage; slot++)
            {
                var deckIndex = pageIndex * perPage + slot;
                if (deckIndex >= deck.CardCount)
                {
                    break;
                }

                var row = slot / grid.Columns;
                var col = slot % grid.Columns;

                var xMm = metrics.OriginXmm + col * (metrics.CardWidthMm + layout.Gutter.Value);
                var yMm = metrics.OriginYmm + row * (metrics.CardHeightMm + layout.Gutter.Value);
                var xPx = UnitConverter.ToPixels((decimal)xMm, Unit.Millimeter, resolution, PxRounding.Round);
                var yPx = UnitConverter.ToPixels((decimal)yMm, Unit.Millimeter, resolution, PxRounding.Round);

                frontPlacements.Add(new CardPlacementPlan(deckIndex, row, col, xPx, yPx, cardWidthPx, cardHeightPx));

                var (backRow, backCol) = LayoutMath.MapDuplex(row, col, grid.Rows, grid.Columns, normalizedDuplex);
                var backXMm = metrics.OriginXmm + backCol * (metrics.CardWidthMm + layout.Gutter.Value);
                var backYMm = metrics.OriginYmm + backRow * (metrics.CardHeightMm + layout.Gutter.Value);
                var backXPx = UnitConverter.ToPixels((decimal)backXMm, Unit.Millimeter, resolution, PxRounding.Round);
                var backYPx = UnitConverter.ToPixels((decimal)backYMm, Unit.Millimeter, resolution, PxRounding.Round);
                backPlacements.Add(new CardPlacementPlan(deckIndex, backRow, backCol, backXPx, backYPx, cardWidthPx, cardHeightPx));
            }

            pages.Add(new LayoutPage(pageIndex + 1, SheetSide.Front, frontPlacements));
            pages.Add(new LayoutPage(pageIndex + 1, SheetSide.Back, backPlacements));
        }

        return new LayoutPlan(grid, pageWidthPx, pageHeightPx, new Mm(metrics.PageWidthMm), new Mm(metrics.PageHeightMm), pages);
    }

    private static DuplexMode NormalizeDuplex(LayoutOptions layout)
    {
        var duplexMode = layout.DuplexMode;
        if (duplexMode == DuplexMode.None && layout.MirrorBackside)
        {
            duplexMode = DuplexMode.MirrorColumns;
        }

        return LayoutMath.NormalizeDuplexMode(duplexMode, layout.Orientation);
    }
}
