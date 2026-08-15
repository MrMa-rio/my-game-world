using System;

namespace MyGameWorld.Shared.Core
{
    [Serializable]
    public readonly struct AssetId : IEquatable<AssetId>, IComparable<AssetId>
    {
        public AssetId(uint value)
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Asset ID zero is reserved.");
            }

            Value = value;
        }

        public uint Value { get; }

        public int CompareTo(AssetId other) => Value.CompareTo(other.Value);

        public bool Equals(AssetId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is AssetId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        public static bool operator ==(AssetId left, AssetId right) => left.Equals(right);

        public static bool operator !=(AssetId left, AssetId right) => !left.Equals(right);
    }
}
