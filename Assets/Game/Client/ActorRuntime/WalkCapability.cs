using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IWalkCapability : IActorCapability
    {
        Vector2 InputDirection { get; }
        float FinalSpeed { get; }
        void SetSpeedModifier(object source, float multiplier);
        void SetSpeedModifier(object source, in MovementModifier modifier);
        void RemoveSpeedModifier(object source);
    }

    [DisallowMultipleComponent]
    public sealed class WalkCapability : ActorCapability, IWalkCapability, IActorIntentHandler<MoveIntent>
    {
        [SerializeField] private WalkProfile _profile;
        private IActorLocomotion _locomotion;
        private readonly MovementSpeedModifiers _speedModifiers = new MovementSpeedModifiers();

        public Vector2 InputDirection { get; private set; }
        public float FinalSpeed => _profile != null ? _speedModifiers.Resolve(_profile.Speed) : 0f;

        public void Configure(WalkProfile profile)
        {
            if (IsInitialized) throw new InvalidOperationException("Walk configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        }

        protected override void OnInitialized()
        {
            if (_profile == null) throw new InvalidOperationException("WalkCapability requires a WalkProfile.");
            if (!Context.Actor.Capabilities.TryGet(out _locomotion))
                throw new InvalidOperationException("WalkCapability requires an IActorLocomotion capability.");
            RegisterIntentHandler<MoveIntent>(this);
        }

        public void HandleIntent(in MoveIntent intent)
        {
            InputDirection = intent.Direction;
            Vector3 right = Context.Transform.right;
            Vector3 forward = Context.Transform.forward;
            right.y = 0f;
            forward.y = 0f;
            right.Normalize();
            forward.Normalize();
            Vector3 worldDirection = right * InputDirection.x + forward * InputDirection.y;
            _locomotion.SetDesiredMotion(Vector3.ClampMagnitude(worldDirection, 1f), FinalSpeed);
        }

        public void SetSpeedModifier(object source, float multiplier)
        {
            _speedModifiers.Set(source, multiplier);
            RefreshMotion();
        }

        public void SetSpeedModifier(object source, in MovementModifier modifier)
        {
            _speedModifiers.Set(source, in modifier);
            RefreshMotion();
        }

        public void RemoveSpeedModifier(object source)
        {
            _speedModifiers.Remove(source);
            RefreshMotion();
        }

        protected override void OnEnabledChanged(bool enabled)
        {
            if (!enabled) Stop();
        }

        protected override void OnReleasing() => Stop();

        private void Stop()
        {
            InputDirection = Vector2.zero;
            _locomotion?.SetDesiredMotion(Vector3.zero, 0f);
        }

        private void RefreshMotion()
        {
            if (!IsInitialized || _locomotion == null) return;
            HandleIntent(new MoveIntent(0, InputDirection));
        }
    }
}
