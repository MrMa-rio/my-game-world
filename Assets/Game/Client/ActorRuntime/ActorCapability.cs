using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IActorCapability
    {
        ActorContext Context { get; }
        bool IsInitialized { get; }
        bool IsEnabled { get; }
        bool CanExecute { get; }
        void Initialize(ActorContext context);
        void SetEnabled(bool enabled);
        void Release();
    }

    public abstract class ActorCapability : MonoBehaviour, IActorCapability
    {
        [SerializeField] private bool _initiallyEnabled = true;

        public ActorContext Context { get; private set; }
        public bool IsInitialized => Context != null;
        public bool IsEnabled { get; private set; }
        public bool CanExecute => IsInitialized && IsEnabled && Context.Actor.State.CanAct;
        public event Action<IActorCapability, bool> EnabledChanged;

        public void Initialize(ActorContext context)
        {
            if (IsInitialized) throw new InvalidOperationException($"Capability {GetType().Name} is already initialized.");
            Context = context ?? throw new ArgumentNullException(nameof(context));
            IsEnabled = _initiallyEnabled;
            try { OnInitialized(); }
            catch
            {
                Context.Actor.Intents.UnregisterOwner(this);
                OnReleasing();
                Context = null;
                IsEnabled = false;
                throw;
            }
        }

        public void SetEnabled(bool enabled)
        {
            if (!IsInitialized) throw new InvalidOperationException($"Capability {GetType().Name} is not initialized.");
            if (IsEnabled == enabled) return;
            IsEnabled = enabled;
            OnEnabledChanged(enabled);
            EnabledChanged?.Invoke(this, enabled);
        }

        public void Release()
        {
            if (!IsInitialized) return;
            ActorContext context = Context;
            context.Actor.Intents.UnregisterOwner(this);
            OnReleasing();
            IsEnabled = false;
            Context = null;
        }

        protected virtual void OnInitialized() { }
        protected virtual void OnEnabledChanged(bool enabled) { }
        protected virtual void OnReleasing() { }

        protected virtual void OnDestroy() => Release();

        protected void RegisterIntentHandler<TIntent>(IActorIntentHandler<TIntent> handler)
            where TIntent : struct, IActorIntent
        {
            if (!IsInitialized) throw new InvalidOperationException("Capability must be initialized before registering intent handlers.");
            Context.Actor.Intents.Register(handler, this);
        }
    }
}
