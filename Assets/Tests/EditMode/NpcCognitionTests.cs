using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.EntityModel;
using MyGameWorld.Shared.NpcCognition;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class NpcCognitionTests
    {
        [Test]
        public void Resolve_HighProfile_UnlocksAdvancedCapabilities()
        {
            IntelligenceCapabilitySet capabilities = new IntelligenceCapabilityResolverV1().Resolve(
                CreateIntelligence(10, 90));

            Assert.That(capabilities.Contains(IntelligenceCapability.Instinct), Is.True);
            Assert.That(capabilities.Contains(IntelligenceCapability.ComplexPlanning), Is.True);
            Assert.That(capabilities.Contains(IntelligenceCapability.ContextualAdaptation), Is.True);
        }

        [Test]
        public void Resolve_LowProfile_DoesNotUnlockSocialBehavior()
        {
            IntelligenceCapabilitySet capabilities = new IntelligenceCapabilityResolverV1().Resolve(
                CreateIntelligence(1, 20));

            Assert.That(capabilities.Contains(IntelligenceCapability.BasicNeeds), Is.True);
            Assert.That(capabilities.Contains(IntelligenceCapability.BasicSocialInteraction), Is.False);
        }

        [Test]
        public void Evaluate_InjectedPolicy_OwnsDecisionSemantics()
        {
            NpcBrain brain = new NpcBrain(
                CreateNpcDNA(),
                new IntelligenceCapabilityResolverV1(),
                new FixedPolicy(73));

            NpcDecision decision = brain.Evaluate(new NpcBrainContext(10, SimulationLod.Simplified));

            Assert.That(decision.Code, Is.EqualTo(73));
        }

        private static IntelligenceDNA CreateIntelligence(byte level, byte trait)
        {
            NormalizedTrait value = new NormalizedTrait(trait);
            return new IntelligenceDNA(level, value, value, value, value, value, value);
        }

        private static NpcDNA CreateNpcDNA()
        {
            EntityDNA entity = new EntityDNA(
                new EntityId(1),
                new ArchetypeId(1),
                77,
                new GeneratorVersion(1),
                new AssetCatalogVersion(1));
            NormalizedTrait trait = new NormalizedTrait(50);
            PersonalityDNA personality = new PersonalityDNA(
                trait, trait, trait, trait, trait, trait, trait, trait);
            return new NpcDNA(entity, CreateIntelligence(5, 50), personality);
        }

        private sealed class FixedPolicy : INpcDecisionPolicy
        {
            private readonly ushort _code;

            public FixedPolicy(ushort code)
            {
                _code = code;
            }

            public NpcDecision Evaluate(
                NpcDNA dna,
                IntelligenceCapabilitySet capabilities,
                NpcBrainContext context)
            {
                return new NpcDecision(_code);
            }
        }
    }
}
