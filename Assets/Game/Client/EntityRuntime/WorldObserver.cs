using System;
using System.Collections.Generic;
using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.EntityRuntime
{
    [Flags]
    public enum WorldObserverPurpose : byte
    {
        Streaming = 1,
        Rendering = 2,
        Physics = 4,
        Environment = 8
    }

    public interface IWorldObserver
    {
        long ObserverId { get; }
        Transform Transform { get; }
        GlobalPosition GlobalPosition { get; }
        float Priority { get; }
        WorldObserverPurpose Purpose { get; }
    }

    public sealed class WorldObserverRegistration : IWorldObserver
    {
        private readonly WorldPresence _presence;
        public WorldObserverRegistration(long observerId, Transform transform, WorldPresence presence,
            float priority, WorldObserverPurpose purpose)
        {
            if (observerId == 0) throw new ArgumentOutOfRangeException(nameof(observerId));
            ObserverId = observerId; Transform = transform != null ? transform : throw new ArgumentNullException(nameof(transform));
            _presence = presence ?? throw new ArgumentNullException(nameof(presence)); Priority = Mathf.Max(0f, priority); Purpose = purpose;
        }
        public long ObserverId { get; }
        public Transform Transform { get; }
        public GlobalPosition GlobalPosition => _presence.GlobalPosition;
        public float Priority { get; }
        public WorldObserverPurpose Purpose { get; }
    }

    public sealed class WorldObserverRegistry
    {
        private readonly Dictionary<long, IWorldObserver> _observers = new Dictionary<long, IWorldObserver>();
        public int Count => _observers.Count;
        public event Action<IWorldObserver> Added;
        public event Action<IWorldObserver> Removed;
        public void Register(IWorldObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (_observers.ContainsKey(observer.ObserverId)) throw new InvalidOperationException($"Observer {observer.ObserverId} is already registered.");
            _observers.Add(observer.ObserverId, observer); Added?.Invoke(observer);
        }
        public bool Unregister(IWorldObserver observer)
        {
            if (observer == null || !_observers.Remove(observer.ObserverId)) return false;
            Removed?.Invoke(observer); return true;
        }
        public bool TryGet(long observerId, out IWorldObserver observer) => _observers.TryGetValue(observerId, out observer);
    }
}
