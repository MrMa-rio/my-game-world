using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Locomotion Profile")]
    public sealed class LocomotionProfile : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _gravity = 22f;
        [SerializeField, Min(0f)] private float _terminalFallSpeed = 45f;
        [SerializeField, Range(0f, 89f)] private float _walkableSlope = 42f;
        [SerializeField, Range(0f, 89f)] private float _difficultSlope = 52f;
        [SerializeField, Range(0f, 89f)] private float _slideSlope = 62f;
        [SerializeField, Range(0.1f, 1f)] private float _difficultSpeedMultiplier = 0.65f;
        [SerializeField, Min(0f)] private float _slideAcceleration = 8f;
        [SerializeField, Min(0.01f)] private float _groundProbeDistance = 0.28f;
        [SerializeField, Min(0.01f)] private float _groundProbeRadius = 0.32f;
        [SerializeField, Min(0f)] private float _stepHeight = 0.42f;
        [SerializeField, Min(0.1f)] private float _planarAcceleration = 18f;
        [SerializeField, Min(0.1f)] private float _planarDeceleration = 24f;

        public float Gravity => _gravity;
        public float TerminalFallSpeed => _terminalFallSpeed;
        public float WalkableSlope => _walkableSlope;
        public float DifficultSlope => Mathf.Max(_walkableSlope, _difficultSlope);
        public float SlideSlope => Mathf.Max(DifficultSlope, _slideSlope);
        public float DifficultSpeedMultiplier => _difficultSpeedMultiplier;
        public float SlideAcceleration => _slideAcceleration;
        public float GroundProbeDistance => _groundProbeDistance;
        public float GroundProbeRadius => _groundProbeRadius;
        public float StepHeight => _stepHeight;
        public float PlanarAcceleration => _planarAcceleration;
        public float PlanarDeceleration => _planarDeceleration;
    }
}
