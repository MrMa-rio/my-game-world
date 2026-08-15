namespace MyGameWorld.Shared.NpcCognition
{
    /// <summary>A transport-neutral decision token. Gameplay systems define code semantics.</summary>
    public readonly struct NpcDecision
    {
        public NpcDecision(ushort code)
        {
            Code = code;
        }

        public ushort Code { get; }

        public static NpcDecision None => default;
    }
}
