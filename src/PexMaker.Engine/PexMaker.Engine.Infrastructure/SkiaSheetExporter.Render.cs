using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed partial class SkiaSheetExporter
{
    private async Task<EncodedPage> RenderPageAsync(
        SheetExportRequest request,
        PageJob job,
        ExportCaches caches,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var placements = job.PageLayout.Placements;
        var renderedCards = new SKBitmap[placements.Count];
        var renderTasks = new List<Task>();

        var maxParallelism = request.EnableParallelism
            ? Math.Min(Math.Max(1, request.MaxDegreeOfParallelism), 4)
            : 1;

        using var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);

        for (var i = 0; i < placements.Count; i++)
        {
            var index = i;
            var placement = placements[index];
            var imageRef = job.IsBackSide ? request.BackImage : request.Cards[placement.DeckIndex];

            if (maxParallelism == 1)
            {
                renderedCards[index] = GetRenderedCardBitmap(request, caches, imageRef, placement);
            }
            else
            {
                renderTasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        renderedCards[index] = GetRenderedCardBitmap(request, caches, imageRef, placement);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }
        }

        if (renderTasks.Count > 0)
        {
            await Task.WhenAll(renderTasks).ConfigureAwait(false);
        }

        var info = new SKImageInfo(request.Layout.PageWidthPx, request.Layout.PageHeightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            var destRect = SKRect.Create(placement.X, placement.Y, placement.Width, placement.Height);
            canvas.DrawBitmap(renderedCards[i], destRect, paint);
        }

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = data.ToArray();

        return new EncodedPage(job.PageIndex, job.IsBackSide, job.OutputPath, bytes);
    }

    private SKBitmap GetRenderedCardBitmap(SheetExportRequest request, ExportCaches caches, ImageRef imageRef, CardPlacementPlan placement)
    {
        var options = new CardRenderOptions(
            placement.Width,
            placement.Height,
            imageRef.FitMode,
            MathEx.Clamp01(imageRef.AnchorX),
            MathEx.Clamp01(imageRef.AnchorY),
            request.BorderEnabled,
            request.BorderThicknessPx,
            request.CornerRadiusPx);

        var key = new RenderCacheKey(
            imageRef.Path,
            placement.Width,
            placement.Height,
            imageRef.FitMode,
            options.AnchorX,
            options.AnchorY,
            request.BorderEnabled,
            request.BorderThicknessPx,
            request.CornerRadiusPx,
            imageRef.RotationDegrees);

        return caches.GetOrAddRenderedCard(key, () =>
        {
            var decoded = caches.GetOrAddDecoded(imageRef.Path, () => DecodeBitmap(imageRef.Path));
            return RenderCardBitmap(decoded, options);
        });
    }

    private SKBitmap DecodeBitmap(string path)
    {
        using var stream = _fileSystem.OpenRead(path);
        var bitmap = SKBitmap.Decode(stream);
        if (bitmap is null)
        {
            throw new InvalidOperationException($"Failed to decode image '{path}'.");
        }

        return bitmap;
    }

    private static SKBitmap RenderCardBitmap(SKBitmap source, CardRenderOptions options)
    {
        var info = new SKImageInfo(options.TargetWidth, options.TargetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var path = new SKPath();
        path.AddRoundRect(SKRect.Create(info.Width, info.Height), (float)options.CornerRadius, (float)options.CornerRadius);
        canvas.ClipPath(path, SKClipOperation.Intersect, true);

        DrawImage(canvas, source, options);

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

        using var snapshot = surface.Snapshot();
        return SKBitmap.FromImage(snapshot);
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

        canvas.DrawBitmap(source, destRect, paint);
    }
}
