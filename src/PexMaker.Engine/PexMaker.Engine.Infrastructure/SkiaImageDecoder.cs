using PexMaker.Engine.Abstractions;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed class SkiaImageDecoder : IImageDecoder
{
    public Task<IImageBuffer> DecodeAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bitmap = SKBitmap.Decode(stream) ?? throw new InvalidOperationException("Failed to decode image.");
        IImageBuffer buffer = new SkiaImageBuffer(bitmap);
        return Task.FromResult(buffer);
    }
}
