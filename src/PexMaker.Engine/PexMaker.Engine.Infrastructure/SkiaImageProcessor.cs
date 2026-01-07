using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed class SkiaImageProcessor : IImageProcessor
{
    public Task<IImageBuffer> RenderCardAsync(IImageBuffer source, CardRenderOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (source is not SkiaImageBuffer skiaBuffer)
        {
            throw new ArgumentException("Unexpected image buffer implementation", nameof(source));
        }

        var info = new SKImageInfo(options.TargetWidth, options.TargetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var path = new SKPath();
        path.AddRoundRect(SKRect.Create(info.Width, info.Height), (float)options.CornerRadius, (float)options.CornerRadius);
        canvas.ClipPath(path, SKClipOperation.Intersect, true);

        DrawImage(canvas, skiaBuffer.Bitmap, options);

        if (options.BorderEnabled && options.BorderThickness > 0)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)options.BorderThickness,
                IsAntialias = true,
            };

            var inset = (float)(options.BorderThickness / 2.0);
            var rect = SKRect.Create(inset, inset, info.Width - (float)options.BorderThickness, info.Height - (float)options.BorderThickness);
            canvas.DrawRoundRect(rect, (float)options.CornerRadius, (float)options.CornerRadius, paint);
        }

        var snapshot = surface.Snapshot();
        var bitmap = SKBitmap.FromImage(snapshot);
        IImageBuffer buffer = new SkiaImageBuffer(bitmap);
        return Task.FromResult(buffer);
    }

    private static void DrawImage(SKCanvas canvas, SKBitmap source, CardRenderOptions options)
    {
        var scaleX = options.TargetWidth / (double)source.Width;
        var scaleY = options.TargetHeight / (double)source.Height;
        var scale = options.FitMode == FitMode.Cover
            ? Math.Max(scaleX, scaleY)
            : Math.Min(scaleX, scaleY);

        var scaledWidth = source.Width * scale;
        var scaledHeight = source.Height * scale;

        var offsetX = (options.TargetWidth - scaledWidth) * options.AnchorX;
        var offsetY = (options.TargetHeight - scaledHeight) * options.AnchorY;

        var destRect = new SKRect((float)offsetX, (float)offsetY, (float)(offsetX + scaledWidth), (float)(offsetY + scaledHeight));

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
        };

        if (Math.Abs(options.RotationDegrees) > 0.01)
        {
            var centerX = options.TargetWidth / 2.0;
            var centerY = options.TargetHeight / 2.0;
            canvas.Save();
            canvas.Translate((float)centerX, (float)centerY);
            canvas.RotateDegrees((float)options.RotationDegrees);
            canvas.Translate((float)-centerX, (float)-centerY);
            canvas.DrawBitmap(source, destRect, paint);
            canvas.Restore();
        }
        else
        {
            canvas.DrawBitmap(source, destRect, paint);
        }
    }
}
