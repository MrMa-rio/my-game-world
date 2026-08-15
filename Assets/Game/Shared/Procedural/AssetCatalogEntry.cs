using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct AssetCatalogEntry : IEquatable<AssetCatalogEntry>
    {
        public AssetCatalogEntry(AssetId assetId, uint selectionWeight)
        {
            if (assetId.Value == 0)
            {
                throw new ArgumentException("A valid asset ID is required.", nameof(assetId));
            }

            if (selectionWeight == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(selectionWeight), "Selection weight must be positive.");
            }

            AssetId = assetId;
            SelectionWeight = selectionWeight;
        }

        public AssetId AssetId { get; }

        public uint SelectionWeight { get; }

        public bool Equals(AssetCatalogEntry other)
        {
            return AssetId == other.AssetId && SelectionWeight == other.SelectionWeight;
        }

        public override bool Equals(object obj) => obj is AssetCatalogEntry other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (AssetId.GetHashCode() * 397) ^ SelectionWeight.GetHashCode();
            }
        }
    }
}
