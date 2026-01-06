using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed class Deck
{
    public Deck(IReadOnlyList<ImageRef> cards, ImageRef back)
    {
        Cards = cards;
        Back = back;
    }

    public IReadOnlyList<ImageRef> Cards { get; }

    public ImageRef Back { get; }

    public int CardCount => Cards.Count;
}
