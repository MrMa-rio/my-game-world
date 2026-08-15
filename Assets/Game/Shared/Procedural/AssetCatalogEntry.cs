using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct AssetCatalogEntry : IEquatable<AssetCatalogEntry>
    {
        public AssetCatalogEntry(AssetId assetId, uint selectionWeight)
            : this(new AssetDescriptor(assetId, AssetCategory.Generic, AssetTrait.None), selectionWeight)
        {
        }

        public AssetCatalogEntry(AssetDescriptor descriptor, uint selectionWeight)
        {
            if (descriptor.AssetId.Value == 0)
            {
                throw new ArgumentException("A valid asset descriptor is required.", nameof(descriptor));
            }

            if (selectionWeight == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(selectionWeight), "Selection weight must be positive.");
            }

            Descriptor = descriptor;
            SelectionWeight = selectionWeight;
        }

        public AssetDescriptor Descriptor { get; }

        public AssetId AssetId => Descriptor.AssetId;

        public uint SelectionWeight { get; }

        public bool Equals(AssetCatalogEntry other)
        {
            return Descriptor.Equals(other.Descriptor) && SelectionWeight == other.SelectionWeight;
        }

        public override bool Equals(object obj) => obj is AssetCatalogEntry other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Descriptor.GetHashCode() * 397) ^ SelectionWeight.GetHashCode();
            }
        }
    }
}
