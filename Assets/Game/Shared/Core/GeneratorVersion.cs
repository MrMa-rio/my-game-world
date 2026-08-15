using System;

namespace MyGameWorld.Shared.Core
{
    [Serializable]
    public readonly struct GeneratorVersion : IEquatable<GeneratorVersion>
    {
        public GeneratorVersion(ushort value)
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Generator version zero is reserved.");
            }

            Value = value;
        }

        public ushort Value { get; }

        public bool Equals(GeneratorVersion other) => Value == other.Value;

        public override bool Equals(object obj) => obj is GeneratorVersion other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();

        public static bool operator ==(GeneratorVersion left, GeneratorVersion right) => left.Equals(right);

        public static bool operator !=(GeneratorVersion left, GeneratorVersion right) => !left.Equals(right);
    }
}
