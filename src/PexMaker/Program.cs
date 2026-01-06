using PexMaker.Engine.Application;
using PexMaker.Engine.Domain;
using PexMaker.Engine.Infrastructure;

Console.WriteLine("PexMaker host (no CLI yet). Engine available.");

public static class DemoHost
{
    public static async Task DemoAsync()
    {
        var engine = PexMakerEngineFactory.CreateDefault();
        var project = new PexProject();
        _ = await engine.ValidateAsync(project, CancellationToken.None).ConfigureAwait(false);
    }
}
