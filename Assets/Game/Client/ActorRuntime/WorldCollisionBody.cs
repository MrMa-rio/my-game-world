using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WorldCollisionBody : MonoBehaviour
    {
        [SerializeField] private PhysicalInteractionLevel _interaction = PhysicalInteractionLevel.Solid;
        [SerializeField, Range(0f, 1f)] private float _movementResistance = 0.15f;
        private Collider _collider;

        public PhysicalInteractionLevel Interaction => _interaction;
        public float MovementResistance => _movementResistance;

        public void Configure(PhysicalInteractionLevel interaction, float movementResistance = 0.15f)
        {
            _interaction = interaction;
            _movementResistance = Mathf.Clamp01(movementResistance);
            Apply();
        }

        private void Awake() => Apply();

        private void Apply()
        {
            _collider = _collider != null ? _collider : GetComponent<Collider>();
            if (_collider == null) throw new InvalidOperationException("WorldCollisionBody requires a Collider.");
            _collider.enabled = _interaction != PhysicalInteractionLevel.None;
            _collider.isTrigger = _interaction == PhysicalInteractionLevel.Soft;
            if (_interaction == PhysicalInteractionLevel.Soft) gameObject.layer = WorldPhysicsLayers.SoftEnvironment;
            else if (_interaction == PhysicalInteractionLevel.Solid && gameObject.layer == 0) gameObject.layer = WorldPhysicsLayers.StaticWorld;
        }
    }
}
