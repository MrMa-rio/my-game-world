using System;

namespace MyGameWorld.Shared.NpcCognition
{
    [Serializable]
    public readonly struct NormalizedTrait : IEquatable<NormalizedTrait>
    {
        public NormalizedTrait(byte value)
        {
            if (value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Trait values must be between 0 and 100.");
            }

            Value = value;
        }

        public byte Value { get; }

        public bool Meets(byte minimum) => Value >= minimum;

        public bool Equals(NormalizedTrait other) => Value == other.Value;

        public override bool Equals(object obj) => obj is NormalizedTrait other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();
    }
}
