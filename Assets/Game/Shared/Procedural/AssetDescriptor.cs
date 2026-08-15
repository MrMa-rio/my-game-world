using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct AssetDescriptor : IEquatable<AssetDescriptor>
    {
        public AssetDescriptor(
            AssetId assetId,
            AssetCategory category,
            AssetTrait traits,
            AssetCompatibility compatibility = default)
        {
            if (assetId.Value == 0)
            {
                throw new ArgumentException("A valid asset ID is required.", nameof(assetId));
            }

            if (!Enum.IsDefined(typeof(AssetCategory), category))
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            AssetId = assetId;
            Category = category;
            Traits = traits;
            Compatibility = compatibility;
        }

        public AssetId AssetId { get; }

        public AssetCategory Category { get; }

        public AssetTrait Traits { get; }

        public AssetCompatibility Compatibility { get; }

        public bool Equals(AssetDescriptor other)
        {
            return AssetId == other.AssetId
                && Category == other.Category
                && Traits == other.Traits
                && Compatibility.Equals(other.Compatibility);
        }

        public override bool Equals(object obj) => obj is AssetDescriptor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = AssetId.GetHashCode();
                hash = (hash * 397) ^ (int)Category;
                hash = (hash * 397) ^ Traits.GetHashCode();
                return (hash * 397) ^ Compatibility.GetHashCode();
            }
        }
    }
}
