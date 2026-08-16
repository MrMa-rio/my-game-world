using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct PhysicalContact
    {
        public PhysicalContact(Collider collider, PhysicalInteractionLevel interaction, Vector3 point,
            Vector3 normal, Vector3 relativeVelocity)
        { Collider = collider; Interaction = interaction; Point = point; Normal = normal; RelativeVelocity = relativeVelocity; }
        public Collider Collider { get; }
        public PhysicalInteractionLevel Interaction { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 RelativeVelocity { get; }
    }

    public interface IPhysicalBody : IActorCapability
    {
        float Mass { get; }
        event Action<PhysicalContact> Contacted;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PhysicalBody : ActorCapability, IPhysicalBody
    {
        [SerializeField] private PhysicalBodyProfile _profile;
        private CharacterController _controller;

        public float Mass => _profile != null ? _profile.Mass : 0f;
        public event Action<PhysicalContact> Contacted;

        public void Configure(PhysicalBodyProfile profile)
        {
            if (IsInitialized) throw new InvalidOperationException("Physical body configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        }

        protected override void OnInitialized()
        {
            if (_profile == null) throw new InvalidOperationException("PhysicalBody requires a PhysicalBodyProfile.");
            _controller = GetComponent<CharacterController>();
            _controller.center = _profile.Center;
            _controller.height = _profile.Height;
            _controller.radius = _profile.Radius;
            _controller.detectCollisions = true;
            gameObject.layer = WorldPhysicsLayers.Actor;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!CanExecute) return;
            Contacted?.Invoke(new PhysicalContact(hit.collider, PhysicalInteractionLevel.Solid,
                hit.point, hit.normal, hit.moveDirection * hit.moveLength));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanExecute) return;
            WorldCollisionBody worldBody = other.GetComponent<WorldCollisionBody>();
            PhysicalInteractionLevel interaction = worldBody != null ? worldBody.Interaction : PhysicalInteractionLevel.Soft;
            Contacted?.Invoke(new PhysicalContact(other, interaction, other.ClosestPoint(transform.position),
                Vector3.zero, Vector3.zero));
        }
    }
}
