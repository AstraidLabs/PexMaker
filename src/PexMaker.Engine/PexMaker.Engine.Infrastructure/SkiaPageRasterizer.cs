using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed class SkiaPageRasterizer : IPageRasterizer
{
    private readonly IFileSystem _fileSystem;

    public SkiaPageRasterizer(IFileSystem fileSystem)
    {
        _fileSystem = Guard.NotNull(fileSystem, nameof(fileSystem));
    }

    public async Task<PageRasterizerResult> RenderPageAsync(PageRasterizerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var caches = new ExportCaches(Math.Max(1, request.MaxCacheItems));
        return await RenderPageAsync(request, caches, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<PageRasterizerResult> RenderPageAsync(
        PageRasterizerRequest request,
        ExportCaches caches,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var placements = request.Page.Placements;
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
            var imageRef = request.Page.Side == SheetSide.Back ? request.BackImage : request.Cards[placement.DeckIndex];

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
        var bleedPx = Math.Max(0, request.BleedPx);

        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            var destRect = SKRect.Create(
                placement.X - bleedPx,
                placement.Y - bleedPx,
                placement.Width + bleedPx * 2,
                placement.Height + bleedPx * 2);
            canvas.DrawBitmap(renderedCards[i], destRect, paint);
        }

        if (request.IncludeCutMarks)
        {
            DrawCutMarks(canvas, placements, request);
        }

        if (request.ShowSafeAreaOverlay)
        {
            DrawSafeAreaOverlay(canvas, placements, request);
        }

        if (request.IncludeRegistrationMarks)
        {
            DrawRegistrationMarks(canvas, placements, request);
        }

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = data.ToArray();

        return new PageRasterizerResult(request.Page.PageNumber, request.Page.Side, bytes);
    }

    private SKBitmap GetRenderedCardBitmap(PageRasterizerRequest request, ExportCaches caches, ImageRef imageRef, CardPlacementPlan placement)
    {
        var bleedPx = Math.Max(0, request.BleedPx);
        var renderWidth = placement.Width + bleedPx * 2;
        var renderHeight = placement.Height + bleedPx * 2;
        var options = new CardRenderOptions(
            renderWidth,
            renderHeight,
            imageRef.FitMode,
            MathEx.Clamp01(imageRef.AnchorX),
            MathEx.Clamp01(imageRef.AnchorY),
            imageRef.RotationDegrees,
            request.BorderEnabled,
            request.BorderThicknessPx,
            request.CornerRadiusPx);

        var key = new RenderCacheKey(
            imageRef.Path,
            renderWidth,
            renderHeight,
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
            return RenderCardBitmap(decoded, options, placement.Width, placement.Height, bleedPx);
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

    private static SKBitmap RenderCardBitmap(SKBitmap source, CardRenderOptions options, int trimWidth, int trimHeight, int bleedPx)
    {
        var info = new SKImageInfo(options.TargetWidth, options.TargetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        DrawImage(canvas, source, options, trimWidth, trimHeight, bleedPx);

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
            var rect = SKRect.Create(
                bleedPx + inset,
                bleedPx + inset,
                trimWidth - (float)options.BorderThickness,
                trimHeight - (float)options.BorderThickness);
            canvas.DrawRoundRect(rect, (float)options.CornerRadius, (float)options.CornerRadius, paint);
        }

        using var snapshot = surface.Snapshot();
        return SKBitmap.FromImage(snapshot);
    }

    private static void DrawImage(SKCanvas canvas, SKBitmap source, CardRenderOptions options, int trimWidth, int trimHeight, int bleedPx)
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
            var centerX = bleedPx + (trimWidth / 2.0);
            var centerY = bleedPx + (trimHeight / 2.0);
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

    private static void DrawCutMarks(SKCanvas canvas, IReadOnlyList<CardPlacementPlan> placements, PageRasterizerRequest request)
    {
        if (request.CutMarkLengthPx <= 0 || request.CutMarkThicknessPx <= 0)
        {
            return;
        }

        var length = (float)request.CutMarkLengthPx;
        var thickness = (float)request.CutMarkThicknessPx;
        var offset = (float)Math.Max(0, request.CutMarkOffsetPx);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = thickness,
        };

        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, request.Layout.PageWidthPx, request.Layout.PageHeightPx), SKClipOperation.Intersect, true);

        var targets = request.CutMarksPerCard
            ? GetPlacementRects(placements)
            : new[] { GetGridRect(placements) };

        foreach (var rect in targets)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            var left = rect.Left;
            var top = rect.Top;
            var right = rect.Right;
            var bottom = rect.Bottom;

            var leftX = left - offset;
            var rightX = right + offset;
            var topY = top - offset;
            var bottomY = bottom + offset;

            canvas.DrawLine(leftX - length, topY, leftX, topY, paint);
            canvas.DrawLine(leftX, topY - length, leftX, topY, paint);

            canvas.DrawLine(rightX, topY, rightX + length, topY, paint);
            canvas.DrawLine(rightX, topY - length, rightX, topY, paint);

            canvas.DrawLine(leftX - length, bottomY, leftX, bottomY, paint);
            canvas.DrawLine(leftX, bottomY, leftX, bottomY + length, paint);

            canvas.DrawLine(rightX, bottomY, rightX + length, bottomY, paint);
            canvas.DrawLine(rightX, bottomY, rightX, bottomY + length, paint);
        }

        canvas.Restore();
    }

    private static void DrawSafeAreaOverlay(SKCanvas canvas, IReadOnlyList<CardPlacementPlan> placements, PageRasterizerRequest request)
    {
        if (request.SafeAreaPx <= 0 || request.SafeAreaOverlayThicknessPx <= 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 120),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = request.SafeAreaOverlayThicknessPx,
        };

        foreach (var placement in placements)
        {
            var rect = SKRect.Create(
                placement.X + request.SafeAreaPx,
                placement.Y + request.SafeAreaPx,
                placement.Width - request.SafeAreaPx * 2,
                placement.Height - request.SafeAreaPx * 2);

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            canvas.DrawRect(rect, paint);
        }
    }

    private static void DrawRegistrationMarks(SKCanvas canvas, IReadOnlyList<CardPlacementPlan> placements, PageRasterizerRequest request)
    {
        if (request.RegistrationMarkSizePx <= 0 || request.RegistrationMarkThicknessPx <= 0)
        {
            return;
        }

        if (request.RegistrationMarkPlacement != RegistrationMarkPlacement.CornersOutsideGrid)
        {
            return;
        }

        var gridRect = GetGridRect(placements);
        if (gridRect.Width <= 0 || gridRect.Height <= 0)
        {
            return;
        }
        var size = (float)request.RegistrationMarkSizePx;
        var half = size / 2f;
        var offset = (float)Math.Max(0, request.RegistrationMarkOffsetPx);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)request.RegistrationMarkThicknessPx,
        };

        var marks = new[]
        {
            new SKPoint(gridRect.Left - offset - half, gridRect.Top - offset - half),
            new SKPoint(gridRect.Right + offset + half, gridRect.Bottom + offset + half),
        };

        foreach (var center in marks)
        {
            if (!TryClampMark(center, half, request, out var clamped))
            {
                continue;
            }

            var left = clamped.X - half;
            var right = clamped.X + half;
            var top = clamped.Y - half;
            var bottom = clamped.Y + half;
            var markRect = SKRect.Create(left, top, right - left, bottom - top);
            if (markRect.IntersectsWith(gridRect))
            {
                continue;
            }

            canvas.DrawLine(left, clamped.Y, right, clamped.Y, paint);
            canvas.DrawLine(clamped.X, top, clamped.X, bottom, paint);
        }
    }

    private static bool TryClampMark(SKPoint center, float halfSize, PageRasterizerRequest request, out SKPoint clamped)
    {
        var minX = halfSize;
        var minY = halfSize;
        var maxX = request.Layout.PageWidthPx - halfSize;
        var maxY = request.Layout.PageHeightPx - halfSize;

        var x = Math.Clamp(center.X, minX, maxX);
        var y = Math.Clamp(center.Y, minY, maxY);

        clamped = new SKPoint(x, y);
        return x >= minX && x <= maxX && y >= minY && y <= maxY;
    }

    private static IEnumerable<SKRect> GetPlacementRects(IReadOnlyList<CardPlacementPlan> placements)
    {
        foreach (var placement in placements)
        {
            yield return SKRect.Create(placement.X, placement.Y, placement.Width, placement.Height);
        }
    }

    private static SKRect GetGridRect(IReadOnlyList<CardPlacementPlan> placements)
    {
        if (placements.Count == 0)
        {
            return SKRect.Empty;
        }

        var left = placements.Min(p => p.X);
        var top = placements.Min(p => p.Y);
        var right = placements.Max(p => p.X + p.Width);
        var bottom = placements.Max(p => p.Y + p.Height);

        return SKRect.Create(left, top, right - left, bottom - top);
    }
}
