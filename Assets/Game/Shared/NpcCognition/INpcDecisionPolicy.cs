namespace MyGameWorld.Shared.NpcCognition
{
    public interface INpcDecisionPolicy
    {
        NpcDecision Evaluate(
            NpcDNA dna,
            IntelligenceCapabilitySet capabilities,
            NpcBrainContext context);
    }
}
