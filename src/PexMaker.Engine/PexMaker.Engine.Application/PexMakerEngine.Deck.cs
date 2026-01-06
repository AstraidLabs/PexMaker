using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    public async Task<Deck> BuildDeckAsync(PexProject project, int? seed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = await ValidateAsync(project, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Project is not valid. Call ValidateAsync before building the deck.");
        }

        if (project.BackImage is null)
        {
            throw new InvalidOperationException("Back image is required.");
        }

        var cards = new List<ImageRef>(project.PairCount * 2);
        for (var i = 0; i < project.PairCount; i++)
        {
            cards.Add(project.FrontImages[i]);
            cards.Add(project.FrontImages[i]);
        }

        var rng = _randomProvider.Create(seed);
        Shuffle(cards, rng);

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
