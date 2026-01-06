using PexMaker.Engine.Abstractions;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed class SkiaImageBuffer : IImageBuffer
{
    public SkiaImageBuffer(SKBitmap bitmap)
    {
        Bitmap = bitmap;
    }

    public SKBitmap Bitmap { get; }

    public int Width => Bitmap.Width;

    public int Height => Bitmap.Height;

    public ValueTask DisposeAsync()
    {
        Bitmap.Dispose();
        return ValueTask.CompletedTask;
    }
}
