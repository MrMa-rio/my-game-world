using System;

namespace MyGameWorld.Shared.NpcCognition
{
    public sealed class NpcBrain : INpcBrain
    {
        private readonly INpcDecisionPolicy _decisionPolicy;

        public NpcBrain(
            NpcDNA dna,
            IIntelligenceCapabilityResolver capabilityResolver,
            INpcDecisionPolicy decisionPolicy)
        {
            DNA = dna ?? throw new ArgumentNullException(nameof(dna));
            if (capabilityResolver == null)
            {
                throw new ArgumentNullException(nameof(capabilityResolver));
            }

            _decisionPolicy = decisionPolicy ?? throw new ArgumentNullException(nameof(decisionPolicy));
            Capabilities = capabilityResolver.Resolve(dna.Intelligence);
        }

        public NpcDNA DNA { get; }

        public IntelligenceCapabilitySet Capabilities { get; }

        public NpcDecision Evaluate(NpcBrainContext context)
        {
            return _decisionPolicy.Evaluate(DNA, Capabilities, context);
        }
    }
}
