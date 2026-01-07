using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal sealed record LayoutMetrics(
    double PageWidthMm,
    double PageHeightMm,
    double UsableWidthMm,
    double UsableHeightMm,
    double CardWidthMm,
    double CardHeightMm,
    int Columns,
    int Rows,
    int PerPage,
    double GridWidthMm,
    double GridHeightMm,
    double OriginXmm,
    double OriginYmm);

internal static class LayoutCalculator
{
    public static LayoutMetrics Calculate(LayoutOptions layout)
    {
        var (pageWidthMm, pageHeightMm) = PageSizeResolver.ResolvePageSize(layout);
        var usableWidth = pageWidthMm - (layout.MarginLeft.Value + layout.MarginRight.Value);
        var usableHeight = pageHeightMm - (layout.MarginTop.Value + layout.MarginBottom.Value);

        var gutter = layout.Gutter.Value;
        var useAutoFit = layout.AutoFitMode != AutoFitMode.None && layout.Grid is not null;

        double cardWidth;
        double cardHeight;
        int columns;
        int rows;

        if (useAutoFit)
        {
            columns = layout.Grid!.Columns;
            rows = layout.Grid!.Rows;
            cardWidth = columns > 0 ? (usableWidth - (columns - 1) * gutter) / columns : 0;
            cardHeight = rows > 0 ? (usableHeight - (rows - 1) * gutter) / rows : 0;

            if (layout.PreferSquareCards)
            {
                var size = Math.Min(cardWidth, cardHeight);
                cardWidth = size;
                cardHeight = size;
            }
        }
        else
        {
            cardWidth = layout.CardWidth.Value;
            cardHeight = layout.CardHeight.Value;
            var grid = LayoutMath.ComputeGrid(usableWidth, usableHeight, cardWidth, cardHeight, gutter);
            columns = grid.Columns;
            rows = grid.Rows;
        }

        var perPage = columns * rows;
        var gridWidth = columns > 0 ? (columns * cardWidth) + ((columns - 1) * gutter) : 0;
        var gridHeight = rows > 0 ? (rows * cardHeight) + ((rows - 1) * gutter) : 0;
        var freeWidth = usableWidth - gridWidth;
        var freeHeight = usableHeight - gridHeight;
        var originX = layout.MarginLeft.Value + (layout.Alignment == GridAlignment.Center ? freeWidth / 2 : 0);
        var originY = layout.MarginTop.Value + (layout.Alignment == GridAlignment.Center ? freeHeight / 2 : 0);

        return new LayoutMetrics(
            pageWidthMm,
            pageHeightMm,
            usableWidth,
            usableHeight,
            cardWidth,
            cardHeight,
            columns,
            rows,
            perPage,
            gridWidth,
            gridHeight,
            originX,
            originY);
    }
}
