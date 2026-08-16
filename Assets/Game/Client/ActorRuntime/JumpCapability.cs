using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IJumpCapability : IActorCapability
    {
        double NextAllowedTime { get; }
    }

    [DisallowMultipleComponent]
    public sealed class JumpCapability : ActorCapability, IJumpCapability, IActorIntentHandler<JumpIntent>
    {
        [SerializeField] private JumpProfile _profile;
        private IActorLocomotion _locomotion;

        public double NextAllowedTime { get; private set; }

        public void Configure(JumpProfile profile)
        {
            if (IsInitialized) throw new InvalidOperationException("Jump configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        }

        protected override void OnInitialized()
        {
            if (_profile == null) throw new InvalidOperationException("JumpCapability requires a JumpProfile.");
            if (!Context.Actor.Capabilities.TryGet(out _locomotion))
                throw new InvalidOperationException("JumpCapability requires an IActorLocomotion capability.");
            RegisterIntentHandler<JumpIntent>(this);
        }

        public void HandleIntent(in JumpIntent intent)
        {
            double now = Time.unscaledTimeAsDouble;
            if (now < NextAllowedTime) return;
            if (_locomotion.TryAddVerticalImpulse(_profile.VerticalSpeed))
                NextAllowedTime = now + _profile.Cooldown;
        }
    }
}
