using System;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum ActorAvailability : byte
    {
        Uninitialized = 0,
        Inactive = 1,
        Active = 2,
        Disabled = 3
    }

    public sealed class ActorState
    {
        public ActorAvailability Availability { get; private set; }
        public bool CanAct => Availability == ActorAvailability.Active;
        public event Action<ActorAvailability, ActorAvailability> AvailabilityChanged;

        internal void SetAvailability(ActorAvailability availability)
        {
            if (Availability == availability) return;
            ActorAvailability previous = Availability;
            Availability = availability;
            AvailabilityChanged?.Invoke(previous, availability);
        }
    }
}
