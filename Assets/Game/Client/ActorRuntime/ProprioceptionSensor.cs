using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum ProprioceptiveMovementState : byte
    {
        Idle = 0,
        Moving = 1,
        Rising = 2,
        Falling = 3,
        Landing = 4
    }

    public readonly struct ProprioceptionSnapshot
    {
        public ProprioceptionSnapshot(Vector3 velocity, Vector3 acceleration, Quaternion orientation,
            Vector3 angularVelocity, bool grounded, float slope, Vector3 movementDirection,
            ProprioceptiveMovementState movementState, int surfaceId = 0)
        {
            Velocity = velocity; Acceleration = acceleration; Orientation = orientation;
            AngularVelocity = angularVelocity; IsGrounded = grounded; Slope = slope;
            MovementDirection = movementDirection; MovementState = movementState; SurfaceId = surfaceId;
        }

        public Vector3 Velocity { get; }
        public Vector3 Acceleration { get; }
        public Quaternion Orientation { get; }
        public Vector3 AngularVelocity { get; }
        public bool IsGrounded { get; }
        public bool IsFalling => MovementState == ProprioceptiveMovementState.Falling;
        public float Slope { get; }
        public Vector3 MovementDirection { get; }
        public ProprioceptiveMovementState MovementState { get; }
        public int SurfaceId { get; }
    }

    public interface IProprioceptionSensor : IActorSensor
    {
        ProprioceptionSnapshot Current { get; }
        event Action<ProprioceptionSnapshot> Sampled;
    }

    [DisallowMultipleComponent]
    public sealed class ProprioceptionSensor : ActorSensor, IProprioceptionSensor
    {
        private IActorLocomotion _locomotion;
        private Vector3 _previousVelocity;
        private Quaternion _previousRotation;
        private bool _hasSample;

        public ProprioceptionSnapshot Current { get; private set; }
        public event Action<ProprioceptionSnapshot> Sampled;

        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.Physics)
                throw new InvalidOperationException("ProprioceptionSensor must use Physics scheduling.");
            if (!Context.Actor.Capabilities.TryGet(out _locomotion))
                throw new InvalidOperationException("ProprioceptionSensor requires an IActorLocomotion capability.");
            _previousRotation = Context.Transform.rotation;
        }

        protected override void Sample()
        {
            LocomotionState locomotion = _locomotion.State;
            Vector3 velocity = locomotion.Velocity;
            float delta = Mathf.Max(0.0001f, SampleDeltaTime);
            Vector3 acceleration = _hasSample ? (velocity - _previousVelocity) / delta : Vector3.zero;
            Quaternion orientation = Context.Transform.rotation;
            Vector3 angularVelocity = _hasSample ? ResolveAngularVelocity(_previousRotation, orientation, delta) : Vector3.zero;
            Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 direction = planar.sqrMagnitude > 0.0001f ? planar.normalized : Vector3.zero;
            ProprioceptiveMovementState movementState = ResolveMovementState(locomotion, planar);
            Current = new ProprioceptionSnapshot(velocity, acceleration, orientation, angularVelocity,
                locomotion.IsGrounded, locomotion.Ground.Angle, direction, movementState, locomotion.Ground.SurfaceId);
            _previousVelocity = velocity;
            _previousRotation = orientation;
            _hasSample = true;
            Sampled?.Invoke(Current);
        }

        private static ProprioceptiveMovementState ResolveMovementState(LocomotionState locomotion, Vector3 planar)
        {
            switch (locomotion.VerticalState)
            {
                case LocomotionVerticalState.Rising: return ProprioceptiveMovementState.Rising;
                case LocomotionVerticalState.Falling: return ProprioceptiveMovementState.Falling;
                case LocomotionVerticalState.Landing: return ProprioceptiveMovementState.Landing;
                default: return planar.sqrMagnitude > 0.0001f ? ProprioceptiveMovementState.Moving : ProprioceptiveMovementState.Idle;
            }
        }

        private static Vector3 ResolveAngularVelocity(Quaternion previous, Quaternion current, float deltaTime)
        {
            Quaternion delta = current * Quaternion.Inverse(previous);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (float.IsNaN(axis.x)) return Vector3.zero;
            return axis * (angle * Mathf.Deg2Rad / deltaTime);
        }
    }
}
