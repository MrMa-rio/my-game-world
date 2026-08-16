using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public sealed class MovementMotor
    {
        private readonly LocomotionProfile _profile;
        public MovementMotor(LocomotionProfile profile) => _profile = profile;
        public Vector3 PlanarVelocity { get; private set; }

        public Vector3 Resolve(Vector3 desiredDirection, float desiredSpeed, GroundProbeResult ground,
            SlopeClassification slope, float deltaTime)
        {
            Vector3 direction = desiredDirection.sqrMagnitude > 1f ? desiredDirection.normalized : desiredDirection;
            direction.y = 0f;
            if (ground.IsGrounded) direction = Vector3.ProjectOnPlane(direction, ground.Normal).normalized * direction.magnitude;
            float multiplier = slope == SlopeClassification.Difficult ? _profile.DifficultSpeedMultiplier : 1f;
            if (slope == SlopeClassification.Blocked) desiredSpeed = 0f;
            Vector3 target = direction * Mathf.Max(0f, desiredSpeed) * multiplier;
            float acceleration = target.sqrMagnitude > PlanarVelocity.sqrMagnitude ? _profile.PlanarAcceleration : _profile.PlanarDeceleration;
            PlanarVelocity = Vector3.MoveTowards(PlanarVelocity, target, acceleration * deltaTime);
            if (slope == SlopeClassification.Slide && ground.IsGrounded)
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, ground.Normal).normalized;
                PlanarVelocity += downhill * _profile.SlideAcceleration * deltaTime;
            }
            return PlanarVelocity;
        }
    }
}
