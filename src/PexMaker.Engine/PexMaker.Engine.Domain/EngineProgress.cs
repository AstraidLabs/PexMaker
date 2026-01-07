namespace PexMaker.Engine.Domain;

public readonly record struct EngineProgress(string Stage, int Current, int Total);
