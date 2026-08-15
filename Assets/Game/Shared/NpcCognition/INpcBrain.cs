namespace MyGameWorld.Shared.NpcCognition
{
    public interface INpcBrain
    {
        NpcDNA DNA { get; }

        IntelligenceCapabilitySet Capabilities { get; }

        NpcDecision Evaluate(NpcBrainContext context);
    }
}
