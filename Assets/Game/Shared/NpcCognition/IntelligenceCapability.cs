namespace MyGameWorld.Shared.NpcCognition
{
    // Numeric values are protocol/persistence identifiers and must never be reordered.
    public enum IntelligenceCapability : byte
    {
        Instinct = 0,
        BasicNeeds = 1,
        SocialRecognition = 2,
        BasicSocialInteraction = 3,
        EventMemory = 4,
        ContextualConversation = 5,
        ShortTermPlanning = 6,
        Inference = 7,
        Negotiation = 8,
        ComplexPlanning = 9,
        SocialStrategy = 10,
        ContextualAdaptation = 11
    }
}
