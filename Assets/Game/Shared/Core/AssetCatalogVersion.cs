using System;

namespace MyGameWorld.Shared.Core
{
    [Serializable]
    public readonly struct AssetCatalogVersion : IEquatable<AssetCatalogVersion>
    {
        public AssetCatalogVersion(ushort value)
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Asset catalog version zero is reserved.");
            }

            Value = value;
        }

        public ushort Value { get; }

        public bool Equals(AssetCatalogVersion other) => Value == other.Value;

        public override bool Equals(object obj) => obj is AssetCatalogVersion other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();

        public static bool operator ==(AssetCatalogVersion left, AssetCatalogVersion right) => left.Equals(right);

        public static bool operator !=(AssetCatalogVersion left, AssetCatalogVersion right) => !left.Equals(right);
    }
}
