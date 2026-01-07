using PexMaker.Engine.Application;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Pdf;

namespace PexMaker.Engine.Infrastructure;

public static class PexMakerEngineFactory
{
    public static PexMakerEngine CreateDefault()
    {
        IFileSystem fileSystem = new FileSystem();
        ISheetExporter exporter = new SkiaSheetExporter(fileSystem);
        IPageRasterizer pageRasterizer = new SkiaPageRasterizer(fileSystem);
        IPdfDocumentExporter pdfExporter = new PdfSharpDocumentExporter(fileSystem);
        IRandomProvider randomProvider = new RandomProvider();
        IProjectSerializer serializer = new JsonProjectSerializer();

        return new PexMakerEngine(fileSystem, exporter, randomProvider, serializer, pageRasterizer, pdfExporter);
    }
}
