using System;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Client.EntityRuntime
{
    [DisallowMultipleComponent]
    public sealed class WorldEntity : MonoBehaviour
    {
        [SerializeField] private long _entityId;
        private IWorldEntityRegistry _registry;
        private bool _registered;

        public EntityId EntityId
        {
            get
            {
                if (!IsInitialized) throw new InvalidOperationException("World entity has not been initialized.");
                return new EntityId(_entityId);
            }
        }

        public bool IsInitialized => Lifecycle.State != WorldEntityLifecycleState.Uninitialized;
        public bool IsRegistered => _registered;
        public WorldEntityLifecycle Lifecycle { get; } = new WorldEntityLifecycle();
        public WorldPresence Presence { get; private set; }

        public void Initialize(EntityId entityId, GlobalPosition globalPosition, WorldCoordinateFrame coordinateFrame,
            IWorldEntityRegistry registry, WorldSpatialContext spatialContext = default)
        {
            if (IsInitialized) throw new InvalidOperationException("World entity is already initialized.");
            _entityId = entityId.Value;
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Presence = new WorldPresence(transform, globalPosition, coordinateFrame, spatialContext);
            Lifecycle.MarkCreated();
        }

        public void Spawn()
        {
            EnsureInitialized();
            EnsureState(WorldEntityLifecycleState.Created);
            _registry.Register(this);
            _registered = true;
            Lifecycle.MarkSpawned();
        }

        public void Activate()
        {
            EnsureInitialized();
            Lifecycle.MarkActive();
        }

        public void DisableEntity()
        {
            EnsureInitialized();
            Lifecycle.MarkDisabled();
        }

        public void BeginDespawn()
        {
            EnsureInitialized();
            Lifecycle.MarkDespawning();
        }

        public void CompleteDespawn()
        {
            EnsureInitialized();
            EnsureState(WorldEntityLifecycleState.Despawning);
            if (_registered) { _registry.Unregister(this); _registered = false; }
            Lifecycle.MarkDestroyed();
        }

        private void OnDestroy()
        {
            if (_registered) { _registry.Unregister(this); _registered = false; }
            if (IsInitialized && Lifecycle.State != WorldEntityLifecycleState.Destroyed)
            {
                if (Lifecycle.State == WorldEntityLifecycleState.Created) { Lifecycle.MarkDestroyed(); return; }
                if (Lifecycle.State != WorldEntityLifecycleState.Despawning) Lifecycle.MarkDespawning();
                Lifecycle.MarkDestroyed();
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized) throw new InvalidOperationException("World entity has not been initialized.");
        }

        private void EnsureState(WorldEntityLifecycleState expected)
        {
            if (Lifecycle.State != expected)
                throw new InvalidOperationException($"World entity must be {expected}, but is {Lifecycle.State}.");
        }
    }
}
