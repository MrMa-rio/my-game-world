using System;

namespace MyGameWorld.Shared.Core
{
    [Serializable]
    public readonly struct ArchetypeId : IEquatable<ArchetypeId>
    {
        public ArchetypeId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Archetype IDs must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(ArchetypeId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ArchetypeId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();

        public static bool operator ==(ArchetypeId left, ArchetypeId right) => left.Equals(right);

        public static bool operator !=(ArchetypeId left, ArchetypeId right) => !left.Equals(right);
    }
}
