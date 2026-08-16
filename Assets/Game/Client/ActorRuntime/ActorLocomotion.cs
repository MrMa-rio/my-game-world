using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum LocomotionVerticalState : byte
    {
        Grounded = 1,
        Rising = 2,
        Falling = 3,
        Landing = 4
    }

    public readonly struct LocomotionState
    {
        public LocomotionState(Vector3 velocity, GroundProbeResult ground, SlopeClassification slope,
            CollisionFlags collision, LocomotionVerticalState verticalState = LocomotionVerticalState.Falling)
        { Velocity = velocity; Ground = ground; Slope = slope; Collision = collision; VerticalState = verticalState; }
        public Vector3 Velocity { get; }
        public GroundProbeResult Ground { get; }
        public SlopeClassification Slope { get; }
        public CollisionFlags Collision { get; }
        public LocomotionVerticalState VerticalState { get; }
        public bool IsGrounded => Ground.IsGrounded;
        public bool IsFalling => VerticalState == LocomotionVerticalState.Falling;
    }

    public interface IActorLocomotion : IActorCapability
    {
        LocomotionState State { get; }
        void SetDesiredMotion(Vector3 worldDirection, float speed);
        void Simulate(float deltaTime);
        bool TryAddVerticalImpulse(float speed);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ActorLocomotion : ActorCapability, IActorLocomotion
    {
        [SerializeField] private LocomotionProfile _profile;
        [SerializeField] private LayerMask _groundLayers = ~0;
        private ActorLocomotionScheduler _scheduler;
        private CharacterController _controller;
        private GroundProbe _groundProbe;
        private SlopeResolver _slopeResolver;
        private GravityResolver _gravity;
        private StepResolver _steps;
        private CollisionResolver _collisions;
        private MovementMotor _motor;
        private Vector3 _desiredDirection;
        private float _desiredSpeed;
        private bool _hasSimulated;

        public LocomotionState State { get; private set; }

        public void Configure(LocomotionProfile profile, ActorLocomotionScheduler scheduler = null, LayerMask? groundLayers = null)
        {
            if (IsInitialized) throw new InvalidOperationException("Locomotion configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            _scheduler = scheduler;
            if (groundLayers.HasValue) _groundLayers = groundLayers.Value;
        }

        protected override void OnInitialized()
        {
            if (_profile == null) throw new InvalidOperationException("ActorLocomotion requires a LocomotionProfile.");
            _controller = GetComponent<CharacterController>();
            _controller.slopeLimit = _profile.SlideSlope;
            _groundProbe = new GroundProbe(_controller, _profile.GroundProbeDistance, _profile.GroundProbeRadius, _groundLayers);
            _slopeResolver = new SlopeResolver(_profile); _gravity = new GravityResolver(_profile);
            _steps = new StepResolver(_controller, _profile.StepHeight); _collisions = new CollisionResolver();
            _motor = new MovementMotor(_profile); _scheduler?.Register(this);
        }

        public void SetDesiredMotion(Vector3 worldDirection, float speed)
        { _desiredDirection = Vector3.ClampMagnitude(worldDirection, 1f); _desiredSpeed = Mathf.Max(0f, speed); }

        public void Simulate(float deltaTime)
        {
            if (!CanExecute || deltaTime <= 0f) return;
            GroundProbeResult ground = _groundProbe.Sample();
            SlopeClassification slope = ground.IsGrounded
                ? _slopeResolver.Classify(ground.Angle)
                : SlopeClassification.Walkable;
            _steps.Apply(ground.IsGrounded, slope);
            Vector3 planar = _motor.Resolve(_desiredDirection, _desiredSpeed, ground, slope, deltaTime);
            float vertical = _gravity.Step(ground.IsGrounded, _collisions.LastFlags, deltaTime);
            CollisionFlags flags = _collisions.Move(_controller, (planar + Vector3.up * vertical) * deltaTime);
            if ((flags & CollisionFlags.Below) != 0 && vertical < 0f)
                ground = new GroundProbeResult(true, ground.Normal, 0f, ground.Collider, ground.SurfaceId);
            Context.Presence.SetLocalPosition(transform.position);
            bool landed = _hasSimulated && !State.IsGrounded && ground.IsGrounded;
            LocomotionVerticalState verticalState = ResolveVerticalState(ground.IsGrounded, vertical, landed);
            State = new LocomotionState(new Vector3(planar.x, vertical, planar.z), ground, slope, flags, verticalState);
            _hasSimulated = true;
        }

        public bool TryAddVerticalImpulse(float speed)
        {
            if (!CanExecute || !_hasSimulated || !State.IsGrounded || speed <= 0f) return false;
            _gravity.AddVerticalImpulse(speed);
            State = new LocomotionState(new Vector3(State.Velocity.x, speed, State.Velocity.z),
                GroundProbeResult.Airborne, State.Slope, State.Collision, LocomotionVerticalState.Rising);
            return true;
        }

        protected override void OnReleasing()
        { _scheduler?.Unregister(this); _scheduler = null; SetDesiredMotion(Vector3.zero, 0f); }

        private static LocomotionVerticalState ResolveVerticalState(bool grounded, float verticalSpeed, bool landed)
        {
            if (landed) return LocomotionVerticalState.Landing;
            if (grounded) return LocomotionVerticalState.Grounded;
            return verticalSpeed > 0f ? LocomotionVerticalState.Rising : LocomotionVerticalState.Falling;
        }
    }
}
