using System;

namespace MyGameWorld.Shared.World
{
    [Serializable]
    public readonly struct ZoneId : IEquatable<ZoneId>, IComparable<ZoneId>
    {
        public ZoneId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Zone IDs must be positive.");
            }

            Value = value;
        }

        public long Value { get; }

        public int CompareTo(ZoneId other) => Value.CompareTo(other.Value);

        public bool Equals(ZoneId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ZoneId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        public static bool operator ==(ZoneId left, ZoneId right) => left.Equals(right);

        public static bool operator !=(ZoneId left, ZoneId right) => !left.Equals(right);
    }
}
