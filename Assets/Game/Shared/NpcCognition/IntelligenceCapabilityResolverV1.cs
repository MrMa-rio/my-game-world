using System;

namespace MyGameWorld.Shared.NpcCognition
{
    public sealed class IntelligenceCapabilityResolverV1 : IIntelligenceCapabilityResolver
    {
        public const ushort Version = 1;

        public ushort RulesVersion => Version;

        public IntelligenceCapabilitySet Resolve(IntelligenceDNA intelligence)
        {
            if (intelligence == null)
            {
                throw new ArgumentNullException(nameof(intelligence));
            }

            IntelligenceCapabilitySet result = default;
            AddIf(ref result, IntelligenceCapability.Instinct, true);
            AddIf(ref result, IntelligenceCapability.BasicNeeds,
                intelligence.OverallLevel >= 1 && intelligence.Perception.Meets(20));
            AddIf(ref result, IntelligenceCapability.SocialRecognition,
                intelligence.OverallLevel >= 2 && intelligence.Perception.Meets(30));
            AddIf(ref result, IntelligenceCapability.BasicSocialInteraction,
                intelligence.OverallLevel >= 3 && intelligence.Social.Meets(30));
            AddIf(ref result, IntelligenceCapability.EventMemory,
                intelligence.OverallLevel >= 4 && intelligence.Memory.Meets(40));
            AddIf(ref result, IntelligenceCapability.ContextualConversation,
                intelligence.OverallLevel >= 5 && intelligence.Language.Meets(50));
            AddIf(ref result, IntelligenceCapability.ShortTermPlanning,
                intelligence.OverallLevel >= 6 && intelligence.Planning.Meets(55));
            AddIf(ref result, IntelligenceCapability.Inference,
                intelligence.OverallLevel >= 7 && intelligence.Reasoning.Meets(65));
            AddIf(ref result, IntelligenceCapability.Negotiation,
                intelligence.OverallLevel >= 7
                && intelligence.Language.Meets(60)
                && intelligence.Social.Meets(65));
            AddIf(ref result, IntelligenceCapability.ComplexPlanning,
                intelligence.OverallLevel >= 8
                && intelligence.Planning.Meets(75)
                && intelligence.Reasoning.Meets(70));
            AddIf(ref result, IntelligenceCapability.SocialStrategy,
                intelligence.OverallLevel >= 9
                && intelligence.Social.Meets(80)
                && intelligence.Reasoning.Meets(75));
            AddIf(ref result, IntelligenceCapability.ContextualAdaptation,
                intelligence.OverallLevel >= 10
                && intelligence.Perception.Meets(80)
                && intelligence.Reasoning.Meets(85));
            return result;
        }

        private static void AddIf(
            ref IntelligenceCapabilitySet set,
            IntelligenceCapability capability,
            bool condition)
        {
            if (condition)
            {
                set = set.Add(capability);
            }
        }
    }
}
