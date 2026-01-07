using System.Text;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public static class Naming
{
    public static string SafeFileName(string input, char replacement = '_')
    {
        ArgumentNullException.ThrowIfNull(input);
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            builder.Append(invalid.Contains(ch) ? replacement : ch);
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "_" : result;
    }

    public static string PageFileName(string prefix, int pageNumber, ExportImageFormat format)
    {
        var sanitized = SafeFileName(prefix);
        var extension = format switch
        {
            ExportImageFormat.Png => "png",
            ExportImageFormat.Jpeg => "jpg",
            ExportImageFormat.Webp => "webp",
            _ => "img",
        };

        return $"{sanitized}_{pageNumber:000}.{extension}";
    }

    public static string PdfFileName(string? prefix, SheetSide side)
    {
        var baseName = string.IsNullOrWhiteSpace(prefix)
            ? (side == SheetSide.Front ? "front" : "back")
            : SafeFileName(prefix);
        return $"{baseName}.pdf";
    }
}
