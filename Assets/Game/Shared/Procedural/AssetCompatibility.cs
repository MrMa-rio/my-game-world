using System;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct AssetCompatibility : IEquatable<AssetCompatibility>
    {
        public AssetCompatibility(AssetTrait requiredTraits, AssetTrait excludedTraits)
        {
            if ((requiredTraits & excludedTraits) != 0)
            {
                throw new ArgumentException("The same trait cannot be both required and excluded.");
            }

            RequiredTraits = requiredTraits;
            ExcludedTraits = excludedTraits;
        }

        public AssetTrait RequiredTraits { get; }

        public AssetTrait ExcludedTraits { get; }

        public bool Accepts(AssetTrait candidateTraits)
        {
            return (candidateTraits & RequiredTraits) == RequiredTraits
                && (candidateTraits & ExcludedTraits) == 0;
        }

        public bool Equals(AssetCompatibility other)
        {
            return RequiredTraits == other.RequiredTraits && ExcludedTraits == other.ExcludedTraits;
        }

        public override bool Equals(object obj) => obj is AssetCompatibility other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)RequiredTraits * 397) ^ (int)ExcludedTraits;
            }
        }
    }
}
