namespace PexMaker.Engine.Domain;

public static class EngineLimits
{
    public const int MinDpi = 72;
    public const int MaxDpi = 1200;

    // Upper bound for memory cards to prevent extreme allocations in layout and export.
    public const int MaxPairs = 500;
    public const int MaxPages = 500;
    public const int MaxOutputFiles = 2000;
    public const int MaxImagesPerProject = 2048;

    public const double MinMm = 0;
    public const double MaxMm = 5000;

    public const double MaxBleedMm = 10;
    public const double MaxCutMarkLengthMm = 10;
    public const double MaxCutMarkThicknessMm = 2;
    public const double MaxCutMarkOffsetMm = 5;

    public const int MinParallelism = 1;
    public const int MaxParallelism = 16;
    public const int MinBufferedPages = 1;
    public const int MaxBufferedPages = 8;
    public const int MinBufferedCards = 1;
    public const int MaxBufferedCards = 256;
    public const int MinCacheItems = 1;
    public const int MaxCacheItems = 2048;

    public const long MinEstimatedWorkingSetBytes = 32L * 1024 * 1024;
    public const long MaxEstimatedWorkingSetBytes = 2L * 1024 * 1024 * 1024;

    public const long MaxPageBitmapBytes = 512L * 1024 * 1024;

    public const int MaxNamingPrefixLength = 48;
}
