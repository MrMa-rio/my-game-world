using System;

namespace MyGameWorld.Shared.NpcCognition
{
    public readonly struct IntelligenceCapabilitySet : IEquatable<IntelligenceCapabilitySet>
    {
        public IntelligenceCapabilitySet(ulong bits)
        {
            Bits = bits;
        }

        public ulong Bits { get; }

        public bool Contains(IntelligenceCapability capability)
        {
            return (Bits & (1UL << (int)capability)) != 0;
        }

        public IntelligenceCapabilitySet Add(IntelligenceCapability capability)
        {
            return new IntelligenceCapabilitySet(Bits | (1UL << (int)capability));
        }

        public bool Equals(IntelligenceCapabilitySet other) => Bits == other.Bits;

        public override bool Equals(object obj) => obj is IntelligenceCapabilitySet other && Equals(other);

        public override int GetHashCode() => Bits.GetHashCode();
    }
}
