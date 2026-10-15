using System;

namespace LastSupply.Core
{
    public sealed class GameRandom : IRandomSource
    {
        private readonly Random RANDOM;

        public GameRandom()
        {
            RANDOM = new Random();
        }

        public GameRandom(int seed)
        {
            RANDOM = new Random(seed);
        }

        public int NextInt(int minInclusive , int maxExclusive)
        {
            if ( minInclusive >= maxExclusive )
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive) , "최댓값은 최솟값보다 커야함");
            }

            return RANDOM.Next(minInclusive , maxExclusive);
        }
    }
}