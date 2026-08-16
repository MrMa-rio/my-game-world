using System;
using MyGameWorld.Client.EntityRuntime;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldEntity))]
    public sealed class Actor : MonoBehaviour
    {
        private WorldEntity _entity;
        private IActorController _controller;

        public bool IsInitialized { get; private set; }
        public WorldEntity Entity => _entity;
        public ActorContext Context { get; private set; }
        public ActorState State { get; } = new ActorState();
        public CapabilityRegistry Capabilities { get; } = new CapabilityRegistry();
        public SensorHub Sensors { get; } = new SensorHub();
        public ActorIntentRouter Intents { get; private set; }
        public IActorController Controller => _controller;

        public void Initialize(WorldEntity entity = null)
        {
            if (IsInitialized) throw new InvalidOperationException("Actor is already initialized.");
            _entity = entity != null ? entity : GetComponent<WorldEntity>();
            if (_entity == null || !_entity.IsInitialized)
                throw new InvalidOperationException("Actor requires an initialized WorldEntity.");
            Context = new ActorContext(this, _entity);
            Intents = new ActorIntentRouter(this);
            _entity.Lifecycle.StateChanged += OnEntityLifecycleChanged;
            State.SetAvailability(ResolveAvailability(_entity.Lifecycle.State));
            IsInitialized = true;
        }

        public void SetController(IActorController controller)
        {
            EnsureInitialized();
            if (ReferenceEquals(_controller, controller)) return;
            _controller?.Unbind();
            _controller = controller;
            _controller?.Bind(Context);
        }

        public void AddCapability<T>(T capability) where T : class, IActorCapability
        {
            EnsureInitialized();
            if (capability == null) throw new ArgumentNullException(nameof(capability));
            if (Capabilities.Contains<T>())
                throw new InvalidOperationException($"Capability contract {typeof(T).Name} is already registered.");
            capability.Initialize(Context);
            Capabilities.Register(capability);
        }

        public bool RemoveCapability<T>(T capability) where T : class, IActorCapability
        {
            EnsureInitialized();
            if (capability == null || !Capabilities.Remove(capability)) return false;
            capability.Release();
            return true;
        }

        public void AddSensor<T>(T sensor) where T : class, IActorSensor
        {
            EnsureInitialized();
            if (sensor == null) throw new ArgumentNullException(nameof(sensor));
            if (Sensors.Contains<T>())
                throw new InvalidOperationException($"Sensor contract {typeof(T).Name} is already registered.");
            sensor.Initialize(Context);
            Sensors.Register(sensor);
        }

        public bool RemoveSensor<T>(T sensor) where T : class, IActorSensor
        {
            EnsureInitialized();
            if (sensor == null || !Sensors.Remove(sensor)) return false;
            sensor.Release();
            return true;
        }

        private void OnEntityLifecycleChanged(WorldEntityLifecycleState previous, WorldEntityLifecycleState current)
            => State.SetAvailability(ResolveAvailability(current));

        private static ActorAvailability ResolveAvailability(WorldEntityLifecycleState lifecycle)
        {
            switch (lifecycle)
            {
                case WorldEntityLifecycleState.Active: return ActorAvailability.Active;
                case WorldEntityLifecycleState.Disabled:
                case WorldEntityLifecycleState.Despawning:
                case WorldEntityLifecycleState.Destroyed: return ActorAvailability.Disabled;
                default: return ActorAvailability.Inactive;
            }
        }

        private void OnDestroy()
        {
            if (_entity != null) _entity.Lifecycle.StateChanged -= OnEntityLifecycleChanged;
            _controller?.Unbind();
            _controller = null;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized) throw new InvalidOperationException("Actor has not been initialized.");
        }
    }
}
