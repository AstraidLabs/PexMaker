using PexMaker.Engine.Abstractions;

namespace PexMaker.Engine.Infrastructure;

internal sealed class RandomProvider : IRandomProvider
{
    public IRandomSource Create(int? seed = null) => new RandomSource(seed);

    private sealed class RandomSource : IRandomSource
    {
        private readonly Random _random;

        public RandomSource(int? seed)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random(0);
        }

        public int NextInt(int maxExclusive) => _random.Next(maxExclusive);
    }
}
