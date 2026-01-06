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
}
