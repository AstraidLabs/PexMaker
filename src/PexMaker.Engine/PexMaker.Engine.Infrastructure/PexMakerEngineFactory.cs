using PexMaker.Engine.Application;
using PexMaker.Engine.Abstractions;

namespace PexMaker.Engine.Infrastructure;

public static class PexMakerEngineFactory
{
    public static PexMakerEngine CreateDefault()
    {
        IFileSystem fileSystem = new FileSystem();
        IImageDecoder decoder = new SkiaImageDecoder();
        IImageProcessor processor = new SkiaImageProcessor();
        IPageRenderer renderer = new SkiaPageRenderer();
        ISheetExporter exporter = new SkiaSheetExporter();
        IRandomProvider randomProvider = new RandomProvider();
        IProjectSerializer serializer = new ProjectSerializer();

        return new PexMakerEngine(fileSystem, decoder, processor, renderer, exporter, randomProvider, serializer);
    }
}
