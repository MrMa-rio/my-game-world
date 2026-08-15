using MyGameWorld.Shared.Core;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class DeterministicRandomTests
    {
        [Test]
        public void NextUInt64_SameSeed_ProducesSameSequence()
        {
            DeterministicRandom first = new DeterministicRandom(98421);
            DeterministicRandom second = new DeterministicRandom(98421);

            for (int index = 0; index < 32; index++)
            {
                Assert.That(first.NextUInt64(), Is.EqualTo(second.NextUInt64()));
            }
        }

        [Test]
        public void NextUInt64_KnownSeed_MatchesVersionOneGoldenSequence()
        {
            ulong[] expected =
            {
                4748763819330083886UL,
                13373165226130976131UL,
                6256410376181272858UL,
                6407355470263702757UL,
                13726965772563750841UL
            };
            DeterministicRandom random = new DeterministicRandom(98421);

            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(random.NextUInt64(), Is.EqualTo(expected[index]));
            }
        }

        [Test]
        public void NextUInt64_ZeroBound_Throws()
        {
            DeterministicRandom random = new DeterministicRandom(1);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextUInt64(0));
        }

        [Test]
        public void Derive_DifferentScope_ProducesDifferentSeed()
        {
            long npcSeed = SeedDerivation.Derive(100, 1, 55);
            long itemSeed = SeedDerivation.Derive(100, 2, 55);

            Assert.That(npcSeed, Is.Not.EqualTo(itemSeed));
            Assert.That(SeedDerivation.Derive(100, 1, 55), Is.EqualTo(npcSeed));
        }
    }
}
