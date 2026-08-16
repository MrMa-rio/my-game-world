using System;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    [DisallowMultipleComponent]
    public sealed class PlayerWorldObserverSystem : MonoBehaviour
    {
        private WorldObserverRegistry _registry;
        public IWorldObserver Observer { get; private set; }
        public void Initialize(Actor actor, WorldObserverRegistry registry, float priority = 1f)
        {
            if (Observer != null) throw new InvalidOperationException("Player world observer is already initialized.");
            if (actor == null || !actor.IsInitialized) throw new InvalidOperationException("Player world observer requires an initialized Actor.");
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Observer = new WorldObserverRegistration(actor.Entity.EntityId.Value, actor.transform, actor.Context.Presence,
                priority, WorldObserverPurpose.Streaming | WorldObserverPurpose.Rendering |
                WorldObserverPurpose.Physics | WorldObserverPurpose.Environment);
            _registry.Register(Observer);
        }
        public void Release()
        {
            if (Observer != null) _registry?.Unregister(Observer);
            Observer = null; _registry = null;
        }
        private void OnDestroy() => Release();
    }
}
