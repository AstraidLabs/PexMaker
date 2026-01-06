using PexMaker.Engine.Abstractions;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed class SkiaPageRenderer : IPageRenderer
{
    public Task<IImageBuffer> ComposePageAsync(PageRenderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var info = new SKImageInfo(request.PageWidth, request.PageHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        foreach (var placement in request.Placements)
        {
            if (placement.Image is not SkiaImageBuffer buffer)
            {
                throw new ArgumentException("Unexpected image buffer implementation", nameof(request));
            }

            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            var destRect = SKRect.Create(placement.X, placement.Y, placement.Width, placement.Height);
            canvas.DrawBitmap(buffer.Bitmap, destRect, paint);
        }

        // Cut marks intentionally left as a no-op for now. The flag is preserved to keep the API stable.

        var snapshot = surface.Snapshot();
        var bitmap = SKBitmap.FromImage(snapshot);
        IImageBuffer bufferResult = new SkiaImageBuffer(bitmap);
        return Task.FromResult(bufferResult);
    }
}
