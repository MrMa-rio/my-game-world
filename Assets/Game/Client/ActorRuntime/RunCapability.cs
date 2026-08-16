using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IRunCapability : IActorCapability
    {
        bool IsRunning { get; }
    }

    [DisallowMultipleComponent]
    public sealed class RunCapability : ActorCapability, IRunCapability, IActorIntentHandler<RunIntent>
    {
        [SerializeField] private RunProfile _profile;
        private IWalkCapability _walk;

        public bool IsRunning { get; private set; }

        public void Configure(RunProfile profile)
        {
            if (IsInitialized) throw new InvalidOperationException("Run configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        }

        protected override void OnInitialized()
        {
            if (_profile == null) throw new InvalidOperationException("RunCapability requires a RunProfile.");
            if (!Context.Actor.Capabilities.TryGet(out _walk))
                throw new InvalidOperationException("RunCapability requires an IWalkCapability.");
            RegisterIntentHandler<RunIntent>(this);
        }

        public void HandleIntent(in RunIntent intent)
        {
            IsRunning = intent.Requested;
            if (IsRunning)
            {
                MovementModifier modifier = new MovementModifier(_profile.SpeedMultiplier, label: "Run");
                _walk.SetSpeedModifier(this, in modifier);
            }
            else _walk.RemoveSpeedModifier(this);
        }

        protected override void OnEnabledChanged(bool enabled)
        {
            if (!enabled) StopRunning();
        }

        protected override void OnReleasing() => StopRunning();

        private void StopRunning()
        {
            IsRunning = false;
            _walk?.RemoveSpeedModifier(this);
        }
    }
}
