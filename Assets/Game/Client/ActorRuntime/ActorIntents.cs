using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IActorIntent
    {
        ulong Sequence { get; }
    }

    public readonly struct MoveIntent : IActorIntent
    {
        public MoveIntent(ulong sequence, Vector2 direction)
        {
            Sequence = sequence;
            Direction = direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }
        public ulong Sequence { get; }
        public Vector2 Direction { get; }
    }

    public readonly struct LookIntent : IActorIntent
    {
        public LookIntent(ulong sequence, Vector2 delta) { Sequence = sequence; Delta = delta; }
        public ulong Sequence { get; }
        public Vector2 Delta { get; }
    }

    public readonly struct RunIntent : IActorIntent
    {
        public RunIntent(ulong sequence, bool requested) { Sequence = sequence; Requested = requested; }
        public ulong Sequence { get; }
        public bool Requested { get; }
    }

    public readonly struct JumpIntent : IActorIntent
    {
        public JumpIntent(ulong sequence) { Sequence = sequence; }
        public ulong Sequence { get; }
    }

    public readonly struct InteractIntent : IActorIntent
    {
        public InteractIntent(ulong sequence) { Sequence = sequence; }
        public ulong Sequence { get; }
    }

    public readonly struct ChangeCameraIntent : IActorIntent
    {
        public ChangeCameraIntent(ulong sequence) { Sequence = sequence; }
        public ulong Sequence { get; }
    }
}
