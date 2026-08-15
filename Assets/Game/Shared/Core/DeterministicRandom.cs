using System;

namespace MyGameWorld.Shared.Core
{
    /// <summary>SplitMix64 with a fixed algorithm. Changing it requires a new generator version.</summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(long seed)
        {
            _state = unchecked((ulong)seed);
        }

        public ulong NextUInt64()
        {
            ulong value = unchecked(_state += 0x9E3779B97F4A7C15UL);
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            ulong bound = (ulong)exclusiveMax;
            ulong threshold = unchecked(0UL - bound) % bound;
            ulong value;

            do
            {
                value = NextUInt64();
            }
            while (value < threshold);

            return (int)(value % bound);
        }

        public double NextUnitDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
    }
}
