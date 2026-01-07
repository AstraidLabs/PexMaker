using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public static class LayoutMath
{
    public static (int Columns, int Rows, int PerPage) ComputeGrid(double usableWidthMm, double usableHeightMm, double cardWidthMm, double cardHeightMm, double gutterMm)
    {
        if (usableWidthMm <= 0 || usableHeightMm <= 0 || cardWidthMm <= 0 || cardHeightMm <= 0)
        {
            return (0, 0, 0);
        }

        var cols = (int)Math.Floor((usableWidthMm + gutterMm) / (cardWidthMm + gutterMm));
        var rows = (int)Math.Floor((usableHeightMm + gutterMm) / (cardHeightMm + gutterMm));

        cols = Math.Max(0, cols);
        rows = Math.Max(0, rows);

        return (cols, rows, cols * rows);
    }

    public static int MirrorColumn(int column, int columns)
    {
        if (columns <= 0)
        {
            return column;
        }

        return columns - 1 - column;
    }

    public static int MirrorRow(int row, int rows)
    {
        if (rows <= 0)
        {
            return row;
        }

        return rows - 1 - row;
    }

    public static DuplexMode NormalizeDuplexMode(DuplexMode mode, PageOrientation orientation)
    {
        if (mode is DuplexMode.FlipLongEdge or DuplexMode.FlipShortEdge)
        {
            return orientation == PageOrientation.Portrait
                ? mode == DuplexMode.FlipLongEdge ? DuplexMode.MirrorColumns : DuplexMode.MirrorRows
                : mode == DuplexMode.FlipLongEdge ? DuplexMode.MirrorRows : DuplexMode.MirrorColumns;
        }

        return mode;
    }

    public static (int Row, int Column) MapDuplex(int row, int column, int rows, int columns, DuplexMode mode)
    {
        return mode switch
        {
            DuplexMode.MirrorColumns => (row, MirrorColumn(column, columns)),
            DuplexMode.MirrorRows => (MirrorRow(row, rows), column),
            DuplexMode.MirrorBoth => (MirrorRow(row, rows), MirrorColumn(column, columns)),
            _ => (row, column),
        };
    }
}
