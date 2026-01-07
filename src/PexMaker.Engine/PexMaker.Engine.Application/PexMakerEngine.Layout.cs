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
        return BuildLayoutPlan(project, deck);
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

    private LayoutPlan BuildLayoutPlan(PexProject project, Deck deck)
    {
        var layout = project.Layout;
        var (pageWidthMm, pageHeightMm) = ResolvePageSize(layout);
        var pageWidthPx = Units.MmToPx(new Mm(pageWidthMm), project.Dpi);
        var pageHeightPx = Units.MmToPx(new Mm(pageHeightMm), project.Dpi);

        var usableWidth = pageWidthMm - (layout.MarginLeft.Value + layout.MarginRight.Value);
        var usableHeight = pageHeightMm - (layout.MarginTop.Value + layout.MarginBottom.Value);
        var gridTuple = LayoutMath.ComputeGrid(usableWidth, usableHeight, layout.CardWidth.Value, layout.CardHeight.Value, layout.Gutter.Value);
        var grid = new LayoutGrid(gridTuple.Columns, gridTuple.Rows, gridTuple.PerPage);
        if (grid.PerPage < 1)
        {
            throw new InvalidOperationException("Layout cannot be computed because no cards fit on a page.");
        }

        var pages = new List<LayoutPage>();
        var cardWidthPx = Units.MmToPx(layout.CardWidth, project.Dpi);
        var cardHeightPx = Units.MmToPx(layout.CardHeight, project.Dpi);
        var perPage = grid.PerPage;
        var totalPages = (int)Math.Ceiling(deck.CardCount / (double)perPage);

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

                var xMm = layout.MarginLeft.Value + col * (layout.CardWidth.Value + layout.Gutter.Value);
                var yMm = layout.MarginTop.Value + row * (layout.CardHeight.Value + layout.Gutter.Value);
                var xPx = Units.MmToPx(new Mm(xMm), project.Dpi);
                var yPx = Units.MmToPx(new Mm(yMm), project.Dpi);

                frontPlacements.Add(new CardPlacementPlan(deckIndex, row, col, xPx, yPx, cardWidthPx, cardHeightPx));

                var mirroredCol = layout.MirrorBackside ? LayoutMath.MirrorColumn(col, grid.Columns) : col;
                var backXMm = layout.MarginLeft.Value + mirroredCol * (layout.CardWidth.Value + layout.Gutter.Value);
                var backXPx = Units.MmToPx(new Mm(backXMm), project.Dpi);
                backPlacements.Add(new CardPlacementPlan(deckIndex, row, mirroredCol, backXPx, yPx, cardWidthPx, cardHeightPx));
            }

            pages.Add(new LayoutPage(pageIndex + 1, SheetSide.Front, frontPlacements));
            pages.Add(new LayoutPage(pageIndex + 1, SheetSide.Back, backPlacements));
        }

        return new LayoutPlan(grid, pageWidthPx, pageHeightPx, new Mm(pageWidthMm), new Mm(pageHeightMm), pages);
    }
}
