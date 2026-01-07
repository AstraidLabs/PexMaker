using PexMaker.Engine.Domain;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed partial class SkiaSheetExporter
{
    internal readonly record struct RenderCacheKey(
        string Path,
        int Width,
        int Height,
        FitMode FitMode,
        double AnchorX,
        double AnchorY,
        bool BorderEnabled,
        double BorderThickness,
        double CornerRadius,
        double RotationDegrees);

    internal sealed class ExportCaches : IDisposable
    {
        private readonly int _maxItems;
        private readonly object _lock = new();
        private readonly Dictionary<string, SKBitmap> _decoded = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> _decodedOrder = new();
        private readonly Dictionary<RenderCacheKey, SKBitmap> _rendered = new();
        private readonly Queue<RenderCacheKey> _renderedOrder = new();

        public ExportCaches(int maxItems)
        {
            _maxItems = Math.Max(1, maxItems);
        }

        public bool TryGetDecoded(string path, out SKBitmap bitmap)
        {
            lock (_lock)
            {
                return _decoded.TryGetValue(path, out bitmap!);
            }
        }

        public SKBitmap GetOrAddDecoded(string path, Func<SKBitmap> factory)
        {
            lock (_lock)
            {
                if (_decoded.TryGetValue(path, out var cached))
                {
                    return cached;
                }
            }

            var created = factory();

            lock (_lock)
            {
                if (_decoded.TryGetValue(path, out var cached))
                {
                    created.Dispose();
                    return cached;
                }

                _decoded[path] = created;
                _decodedOrder.Enqueue(path);
                EvictDecodedIfNeeded();
                return created;
            }
        }

        public SKBitmap GetOrAddRenderedCard(RenderCacheKey key, Func<SKBitmap> factory)
        {
            lock (_lock)
            {
                if (_rendered.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var created = factory();

            lock (_lock)
            {
                if (_rendered.TryGetValue(key, out var cached))
                {
                    created.Dispose();
                    return cached;
                }

                _rendered[key] = created;
                _renderedOrder.Enqueue(key);
                EvictRenderedIfNeeded();
                return created;
            }
        }

        private void EvictDecodedIfNeeded()
        {
            while (_decoded.Count > _maxItems && _decodedOrder.Count > 0)
            {
                var oldest = _decodedOrder.Dequeue();
                if (_decoded.Remove(oldest, out var bitmap))
                {
                    bitmap.Dispose();
                }
            }
        }

        private void EvictRenderedIfNeeded()
        {
            while (_rendered.Count > _maxItems && _renderedOrder.Count > 0)
            {
                var oldest = _renderedOrder.Dequeue();
                if (_rendered.Remove(oldest, out var bitmap))
                {
                    bitmap.Dispose();
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var bitmap in _decoded.Values)
                {
                    bitmap.Dispose();
                }

                foreach (var bitmap in _rendered.Values)
                {
                    bitmap.Dispose();
                }

                _decoded.Clear();
                _rendered.Clear();
                _decodedOrder.Clear();
                _renderedOrder.Clear();
            }
        }
    }
}
