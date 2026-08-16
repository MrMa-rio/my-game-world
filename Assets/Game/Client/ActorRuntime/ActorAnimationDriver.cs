using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum ActorAnimationMovementState : byte
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Jump = 3,
        Fall = 4,
        Land = 5
    }

    public readonly struct ActorAnimationState
    {
        public ActorAnimationState(ActorAnimationMovementState movement, float planarSpeed,
            float verticalSpeed, bool grounded, float slope)
        { Movement = movement; PlanarSpeed = planarSpeed; VerticalSpeed = verticalSpeed; Grounded = grounded; Slope = slope; }
        public ActorAnimationMovementState Movement { get; }
        public float PlanarSpeed { get; }
        public float VerticalSpeed { get; }
        public bool Grounded { get; }
        public float Slope { get; }
    }

    public interface IActorAnimationSink
    {
        void Apply(in ActorAnimationState state);
    }

    [CreateAssetMenu(menuName = "My Game World/Actor/Animation Driver Profile")]
    public sealed class ActorAnimationDriverProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _movingThreshold = 0.1f;
        [SerializeField, Min(0f)] private float _runThreshold = 5.5f;
        [SerializeField] private string _movementStateParameter = "MovementState";
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private string _verticalSpeedParameter = "VerticalSpeed";
        [SerializeField] private string _groundedParameter = "Grounded";
        [SerializeField] private string _slopeParameter = "Slope";
        public float MovingThreshold => _movingThreshold;
        public float RunThreshold => Mathf.Max(_movingThreshold, _runThreshold);
        public int MovementStateHash => Animator.StringToHash(_movementStateParameter);
        public int SpeedHash => Animator.StringToHash(_speedParameter);
        public int VerticalSpeedHash => Animator.StringToHash(_verticalSpeedParameter);
        public int GroundedHash => Animator.StringToHash(_groundedParameter);
        public int SlopeHash => Animator.StringToHash(_slopeParameter);
    }

    public sealed class AnimatorAnimationSink : IActorAnimationSink
    {
        private readonly Animator _animator;
        private readonly ActorAnimationDriverProfile _profile;
        public AnimatorAnimationSink(Animator animator, ActorAnimationDriverProfile profile)
        { _animator = animator != null ? animator : throw new ArgumentNullException(nameof(animator)); _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile)); }
        public void Apply(in ActorAnimationState state)
        {
            _animator.SetInteger(_profile.MovementStateHash, (int)state.Movement);
            _animator.SetFloat(_profile.SpeedHash, state.PlanarSpeed);
            _animator.SetFloat(_profile.VerticalSpeedHash, state.VerticalSpeed);
            _animator.SetBool(_profile.GroundedHash, state.Grounded);
            _animator.SetFloat(_profile.SlopeHash, state.Slope);
        }
    }

    [DisallowMultipleComponent]
    public sealed class ActorAnimationDriver : MonoBehaviour
    {
        private IProprioceptionSensor _proprioception;
        private IActorAnimationSink _sink;
        private ActorAnimationDriverProfile _profile;
        public bool IsInitialized => _proprioception != null;
        public ActorAnimationState Current { get; private set; }

        public void Initialize(Actor actor, ActorAnimationDriverProfile profile, IActorAnimationSink sink)
        {
            if (IsInitialized) throw new InvalidOperationException("Animation driver is already initialized.");
            if (actor == null || !actor.Sensors.TryGet(out _proprioception))
                throw new InvalidOperationException("Animation driver requires an Actor with proprioception.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _proprioception.Sampled += OnProprioceptionSampled;
        }

        public void Release()
        {
            if (_proprioception != null) _proprioception.Sampled -= OnProprioceptionSampled;
            _proprioception = null; _sink = null; _profile = null;
        }

        private void OnProprioceptionSampled(ProprioceptionSnapshot snapshot)
        {
            float planarSpeed = new Vector2(snapshot.Velocity.x, snapshot.Velocity.z).magnitude;
            ActorAnimationMovementState movement = ResolveMovement(snapshot, planarSpeed);
            Current = new ActorAnimationState(movement, planarSpeed, snapshot.Velocity.y, snapshot.IsGrounded, snapshot.Slope);
            ActorAnimationState state = Current;
            _sink.Apply(in state);
        }

        private ActorAnimationMovementState ResolveMovement(ProprioceptionSnapshot snapshot, float planarSpeed)
        {
            switch (snapshot.MovementState)
            {
                case ProprioceptiveMovementState.Rising: return ActorAnimationMovementState.Jump;
                case ProprioceptiveMovementState.Falling: return ActorAnimationMovementState.Fall;
                case ProprioceptiveMovementState.Landing: return ActorAnimationMovementState.Land;
            }
            if (planarSpeed < _profile.MovingThreshold) return ActorAnimationMovementState.Idle;
            return planarSpeed >= _profile.RunThreshold ? ActorAnimationMovementState.Run : ActorAnimationMovementState.Walk;
        }

        private void OnDestroy() => Release();
    }
}
