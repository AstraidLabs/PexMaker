using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    public async Task<LayoutPlan> ComputeLayoutAsync(PexProject project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = await ValidateAsync(project, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Project is not valid. Call ValidateAsync before computing layout.");
        }

        if (project.BackImage is null)
        {
            throw new InvalidOperationException("Back image is required.");
        }

        var deck = BuildSequentialDeck(project);
        var metrics = LayoutCalculator.Calculate(project.Layout);
        return BuildLayoutPlan(project, deck, metrics);
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
        var pageWidthPx = Units.MmToPx(new Mm(metrics.PageWidthMm), project.Dpi);
        var pageHeightPx = Units.MmToPx(new Mm(metrics.PageHeightMm), project.Dpi);

        var grid = new LayoutGrid(metrics.Columns, metrics.Rows, metrics.PerPage);
        if (grid.PerPage < 1 || metrics.CardWidthMm <= 0 || metrics.CardHeightMm <= 0)
        {
            throw new InvalidOperationException("Layout cannot be computed because no cards fit on a page.");
        }

        var pages = new List<LayoutPage>();
        var cardWidthPx = Units.MmToPx(new Mm(metrics.CardWidthMm), project.Dpi);
        var cardHeightPx = Units.MmToPx(new Mm(metrics.CardHeightMm), project.Dpi);
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
                var xPx = Units.MmToPx(new Mm(xMm), project.Dpi);
                var yPx = Units.MmToPx(new Mm(yMm), project.Dpi);

                frontPlacements.Add(new CardPlacementPlan(deckIndex, row, col, xPx, yPx, cardWidthPx, cardHeightPx));

                var (backRow, backCol) = LayoutMath.MapDuplex(row, col, grid.Rows, grid.Columns, normalizedDuplex);
                var backXMm = metrics.OriginXmm + backCol * (metrics.CardWidthMm + layout.Gutter.Value);
                var backYMm = metrics.OriginYmm + backRow * (metrics.CardHeightMm + layout.Gutter.Value);
                var backXPx = Units.MmToPx(new Mm(backXMm), project.Dpi);
                var backYPx = Units.MmToPx(new Mm(backYMm), project.Dpi);
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
