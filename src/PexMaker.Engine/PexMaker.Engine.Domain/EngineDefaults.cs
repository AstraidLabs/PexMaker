namespace PexMaker.Engine.Domain;

public static class EngineDefaults
{
    public const int DefaultDpi = 300;

    public static readonly Mm A4Width = new(210);
    public static readonly Mm A4Height = new(297);
    public static readonly Mm A3Width = new(297);
    public static readonly Mm A3Height = new(420);
    public static readonly Mm LetterWidth = new(215.9);
    public static readonly Mm LetterHeight = new(279.4);

    public static readonly Mm DefaultMargin = new(10);
    public static readonly Mm DefaultGutter = new(3);
    public static readonly Mm DefaultCardWidth = new(63);
    public static readonly Mm DefaultCardHeight = new(88);
    public static readonly Mm DefaultCornerRadius = new(2);
    public static readonly Mm DefaultBorderThickness = new(0.5);
    public static readonly Mm DefaultCutMarkLength = new(3.0);
    public static readonly Mm DefaultCutMarkThickness = new(0.2);
    public static readonly Mm DefaultCutMarkOffset = new(1.0);

    public const PageFormat DefaultPageFormat = PageFormat.A4;
    public const PageOrientation DefaultOrientation = PageOrientation.Portrait;
    public const FitMode DefaultFitMode = FitMode.Cover;
    public const AutoFitMode DefaultAutoFitMode = AutoFitMode.None;
    public const GridAlignment DefaultGridAlignment = GridAlignment.TopLeft;
    public const DuplexMode DefaultDuplexMode = DuplexMode.None;
}
