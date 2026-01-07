namespace PexMaker.Engine.Domain;

public sealed record LayoutGrid(int Columns, int Rows, int PerPage);

public sealed record CardPlacementPlan(int DeckIndex, int Row, int Column, int X, int Y, int Width, int Height);

public sealed record LayoutPage(int PageNumber, SheetSide Side, IReadOnlyList<CardPlacementPlan> Placements);

public sealed class LayoutPlan
{
    public LayoutPlan(LayoutGrid grid, int pageWidthPx, int pageHeightPx, Mm pageWidthMm, Mm pageHeightMm, IReadOnlyList<LayoutPage> pages)
    {
        Grid = grid;
        PageWidthPx = pageWidthPx;
        PageHeightPx = pageHeightPx;
        PageWidthMm = pageWidthMm;
        PageHeightMm = pageHeightMm;
        Pages = pages;
    }

    public LayoutGrid Grid { get; }

    public int PageWidthPx { get; }

    public int PageHeightPx { get; }

    public Mm PageWidthMm { get; }

    public Mm PageHeightMm { get; }

    public IReadOnlyList<LayoutPage> Pages { get; }
}
