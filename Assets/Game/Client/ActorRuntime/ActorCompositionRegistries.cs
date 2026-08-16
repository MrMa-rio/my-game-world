using System;
using System.Collections.Generic;

namespace MyGameWorld.Client.ActorRuntime
{
    internal sealed class ActorComponentRegistry
    {
        private readonly Dictionary<Type, object> _components = new Dictionary<Type, object>();
        public int Count => _components.Count;
        public IEnumerable<Type> Contracts => _components.Keys;
        public bool Contains<T>() where T : class => _components.ContainsKey(typeof(T));

        public void Register<T>(T component) where T : class
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            Type contract = typeof(T);
            if (_components.ContainsKey(contract)) throw new InvalidOperationException($"Actor component contract {contract.Name} is already registered.");
            _components.Add(contract, component);
        }

        public bool TryGet<T>(out T component) where T : class
        {
            object value;
            if (_components.TryGetValue(typeof(T), out value)) { component = (T)value; return true; }
            component = null; return false;
        }

        public bool Remove<T>(T component) where T : class
        {
            object registered;
            if (!_components.TryGetValue(typeof(T), out registered) || !ReferenceEquals(registered, component)) return false;
            return _components.Remove(typeof(T));
        }
    }

    public sealed class CapabilityRegistry
    {
        private readonly ActorComponentRegistry _components = new ActorComponentRegistry();
        public int Count => _components.Count;
        public IEnumerable<Type> ContractTypes => _components.Contracts;
        public bool Contains<T>() where T : class, IActorCapability => _components.Contains<T>();
        public void Register<T>(T capability) where T : class, IActorCapability
        {
            if (capability == null) throw new ArgumentNullException(nameof(capability));
            if (!capability.IsInitialized) throw new InvalidOperationException("A capability must be initialized before registration.");
            _components.Register(capability);
        }
        public bool TryGet<T>(out T capability) where T : class, IActorCapability => _components.TryGet(out capability);
        public bool Remove<T>(T capability) where T : class, IActorCapability => _components.Remove(capability);
        public bool SetEnabled<T>(bool enabled) where T : class, IActorCapability
        {
            T capability;
            if (!TryGet(out capability)) return false;
            capability.SetEnabled(enabled); return true;
        }
    }

    public sealed class SensorHub
    {
        private readonly ActorComponentRegistry _sensors = new ActorComponentRegistry();
        public int Count => _sensors.Count;
        public IEnumerable<Type> ContractTypes => _sensors.Contracts;
        public bool Contains<T>() where T : class, IActorSensor => _sensors.Contains<T>();
        public void Register<T>(T sensor) where T : class, IActorSensor
        {
            if (sensor == null) throw new ArgumentNullException(nameof(sensor));
            if (!sensor.IsInitialized) throw new InvalidOperationException("A sensor must be initialized before registration.");
            _sensors.Register(sensor);
        }
        public bool TryGet<T>(out T sensor) where T : class, IActorSensor => _sensors.TryGet(out sensor);
        public bool Remove<T>(T sensor) where T : class, IActorSensor => _sensors.Remove(sensor);
    }
}
