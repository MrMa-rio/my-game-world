using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum SlopeClassification : byte { Walkable = 1, Difficult = 2, Slide = 3, Blocked = 4 }

    public sealed class SlopeResolver
    {
        private readonly LocomotionProfile _profile;
        public SlopeResolver(LocomotionProfile profile) => _profile = profile;
        public SlopeClassification Classify(float angle)
        {
            if (angle <= _profile.WalkableSlope) return SlopeClassification.Walkable;
            if (angle <= _profile.DifficultSlope) return SlopeClassification.Difficult;
            if (angle <= _profile.SlideSlope) return SlopeClassification.Slide;
            return SlopeClassification.Blocked;
        }
        public Vector3 ResolveDirection(Vector3 desired, Vector3 groundNormal)
            => Vector3.ProjectOnPlane(desired, groundNormal).normalized;
    }

    public sealed class GravityResolver
    {
        private readonly LocomotionProfile _profile;
        public GravityResolver(LocomotionProfile profile) => _profile = profile;
        public float VerticalVelocity { get; private set; }
        public float Step(bool grounded, CollisionFlags previousCollision, float deltaTime)
        {
            if (grounded && VerticalVelocity <= 0f) VerticalVelocity = -2f;
            else VerticalVelocity = Mathf.Max(-_profile.TerminalFallSpeed, VerticalVelocity - _profile.Gravity * deltaTime);
            if ((previousCollision & CollisionFlags.Above) != 0 && VerticalVelocity > 0f) VerticalVelocity = 0f;
            return VerticalVelocity;
        }
        public void AddVerticalImpulse(float speed) => VerticalVelocity = Mathf.Max(VerticalVelocity, speed);
    }

    public sealed class StepResolver
    {
        private readonly CharacterController _controller;
        private readonly float _stepHeight;
        public StepResolver(CharacterController controller, float stepHeight) { _controller = controller; _stepHeight = stepHeight; }
        public void Apply(bool grounded, SlopeClassification slope)
            => _controller.stepOffset = grounded && slope != SlopeClassification.Blocked ? Mathf.Min(_stepHeight, _controller.height * 0.45f) : 0f;
    }

    public sealed class CollisionResolver
    {
        public CollisionFlags LastFlags { get; private set; }
        public CollisionFlags Move(CharacterController controller, Vector3 displacement)
        { LastFlags = controller.Move(displacement); return LastFlags; }
    }
}
