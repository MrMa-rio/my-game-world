using System;
using MyGameWorld.Client.EntityRuntime;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct TouchEvent
    {
        public TouchEvent(GameObject source, Vector3 contactPoint, int surfaceId, Vector3 normal,
            float force, Vector3 relativeVelocity, PhysicalInteractionLevel interaction)
        {
            Source = source; ContactPoint = contactPoint; SurfaceId = surfaceId; Normal = normal;
            Force = force; RelativeVelocity = relativeVelocity; Interaction = interaction;
        }
        public GameObject Source { get; }
        public Vector3 ContactPoint { get; }
        public int SurfaceId { get; }
        public Vector3 Normal { get; }
        public float Force { get; }
        public Vector3 RelativeVelocity { get; }
        public PhysicalInteractionLevel Interaction { get; }
    }

    public interface ITouchSensor : IActorSensor
    {
        TouchEvent LastContact { get; }
        event Action<TouchEvent> ContactSensed;
    }

    [DisallowMultipleComponent]
    public sealed class TouchSensor : ActorSensor, ITouchSensor
    {
        private IPhysicalBody _physicalBody;

        public TouchEvent LastContact { get; private set; }
        public event Action<TouchEvent> ContactSensed;

        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.EventDriven)
                throw new InvalidOperationException("TouchSensor must be event-driven.");
            if (!Context.Actor.Capabilities.TryGet(out _physicalBody))
                throw new InvalidOperationException("TouchSensor requires an IPhysicalBody capability.");
            _physicalBody.Contacted += OnPhysicalContact;
        }

        protected override void OnReleasing()
        {
            if (_physicalBody != null) _physicalBody.Contacted -= OnPhysicalContact;
            _physicalBody = null;
        }

        protected override void Sample() { }

        private void OnPhysicalContact(PhysicalContact contact)
        {
            if (!IsEnabled || !Context.Actor.State.CanAct) return;
            int surfaceId = 0;
            if (contact.Collider != null)
            {
                IPhysicalSurfaceProvider provider = contact.Collider.GetComponent<IPhysicalSurfaceProvider>();
                if (provider != null) surfaceId = provider.SurfaceId;
            }
            float force = _physicalBody.Mass * contact.RelativeVelocity.magnitude;
            LastContact = new TouchEvent(contact.Collider != null ? contact.Collider.gameObject : null,
                contact.Point, surfaceId, contact.Normal, force, contact.RelativeVelocity, contact.Interaction);
            ContactSensed?.Invoke(LastContact);
        }
    }
}
