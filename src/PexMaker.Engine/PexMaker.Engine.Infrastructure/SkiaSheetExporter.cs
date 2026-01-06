using PexMaker.Engine.Abstractions;
using SkiaSharp;

namespace PexMaker.Engine.Infrastructure;

internal sealed class SkiaSheetExporter : ISheetExporter
{
    public async Task<IReadOnlyList<string>> ExportAsync(SheetExportRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var files = new List<string>();
        foreach (var sheet in request.Sheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sheet.Image is not SkiaImageBuffer buffer)
            {
                throw new ArgumentException("Unexpected image buffer implementation", nameof(request));
            }

            var format = ToImageFormat(request.Format);
            using var image = SKImage.FromBitmap(buffer.Bitmap);
            using var data = image.Encode(format, 90);
            var path = Path.Combine(request.Directory, sheet.FileName);
            await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            data.SaveTo(stream);
            files.Add(path);
        }

        return files;
    }

    private static SKEncodedImageFormat ToImageFormat(ExportImageFormat format) => format switch
    {
        ExportImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
        ExportImageFormat.Webp => SKEncodedImageFormat.Webp,
        _ => SKEncodedImageFormat.Png,
    };
}
