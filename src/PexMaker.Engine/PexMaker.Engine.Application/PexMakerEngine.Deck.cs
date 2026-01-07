using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    /// <summary>
    /// Builds a shuffled deck after validating and normalizing project inputs.
    /// </summary>
    public Task<EngineOperationResult<Deck>> BuildDeckAsync(PexProject project, int? seed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateProject(project, out var normalizedProject);
        if (!validation.IsValid)
        {
            return Task.FromResult(new EngineOperationResult<Deck>(null, validation));
        }

        var deck = BuildShuffledDeck(normalizedProject, seed);
        return Task.FromResult(new EngineOperationResult<Deck>(deck, validation));
    }

    private Deck BuildShuffledDeck(PexProject project, int? seed)
    {
        var cards = new List<ImageRef>(project.PairCount * 2);
        for (var i = 0; i < project.PairCount; i++)
        {
            cards.Add(project.FrontImages[i]);
            cards.Add(project.FrontImages[i]);
        }

        var rng = _randomProvider.Create(seed);
        Shuffle(cards, rng);

        if (project.BackImage is null)
        {
            throw new InvalidOperationException("Back image is required.");
        }

        return new Deck(cards, project.BackImage);
    }

    private static void Shuffle<T>(IList<T> list, IRandomSource random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
