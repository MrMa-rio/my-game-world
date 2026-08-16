using System;

namespace MyGameWorld.Client.EntityRuntime
{
    public enum WorldEntityLifecycleState : byte
    {
        Uninitialized = 0,
        Created = 1,
        Spawned = 2,
        Active = 3,
        Disabled = 4,
        Despawning = 5,
        Destroyed = 6
    }

    public sealed class WorldEntityLifecycle
    {
        public WorldEntityLifecycleState State { get; private set; }

        public event Action<WorldEntityLifecycleState, WorldEntityLifecycleState> StateChanged;

        public void MarkCreated() => Transition(WorldEntityLifecycleState.Created);
        public void MarkSpawned() => Transition(WorldEntityLifecycleState.Spawned);
        public void MarkActive() => Transition(WorldEntityLifecycleState.Active);
        public void MarkDisabled() => Transition(WorldEntityLifecycleState.Disabled);
        public void MarkDespawning() => Transition(WorldEntityLifecycleState.Despawning);
        public void MarkDestroyed() => Transition(WorldEntityLifecycleState.Destroyed);

        private void Transition(WorldEntityLifecycleState next)
        {
            if (!CanTransition(State, next))
            {
                throw new InvalidOperationException($"World entity lifecycle cannot transition from {State} to {next}.");
            }

            WorldEntityLifecycleState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }

        public static bool CanTransition(WorldEntityLifecycleState current, WorldEntityLifecycleState next)
        {
            switch (current)
            {
                case WorldEntityLifecycleState.Uninitialized:
                    return next == WorldEntityLifecycleState.Created;
                case WorldEntityLifecycleState.Created:
                    return next == WorldEntityLifecycleState.Spawned || next == WorldEntityLifecycleState.Destroyed;
                case WorldEntityLifecycleState.Spawned:
                    return next == WorldEntityLifecycleState.Active || next == WorldEntityLifecycleState.Despawning;
                case WorldEntityLifecycleState.Active:
                    return next == WorldEntityLifecycleState.Disabled || next == WorldEntityLifecycleState.Despawning;
                case WorldEntityLifecycleState.Disabled:
                    return next == WorldEntityLifecycleState.Active || next == WorldEntityLifecycleState.Despawning;
                case WorldEntityLifecycleState.Despawning:
                    return next == WorldEntityLifecycleState.Destroyed;
                default:
                    return false;
            }
        }
    }
}
