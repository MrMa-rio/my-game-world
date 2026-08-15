namespace MyGameWorld.Shared.NpcCognition
{
    public interface IIntelligenceCapabilityResolver
    {
        ushort RulesVersion { get; }

        IntelligenceCapabilitySet Resolve(IntelligenceDNA intelligence);
    }
}
