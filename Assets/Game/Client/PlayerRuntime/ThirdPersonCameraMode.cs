using System;
using MyGameWorld.Client.ActorRuntime;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Player/Third Person Camera Profile")]
    public sealed class ThirdPersonCameraProfile : ScriptableObject
    {
        [SerializeField, Min(0.5f)] private float _distance = 5f;
        [SerializeField, Min(0f)] private float _pivotHeight = 1.6f;
        [SerializeField, Range(-80f, 0f)] private float _minimumPitch = -35f;
        [SerializeField, Range(0f, 80f)] private float _maximumPitch = 65f;
        [SerializeField, Min(0f)] private float _positionSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float _rotationSharpness = 14f;
        public float Distance => _distance;
        public float PivotHeight => _pivotHeight;
        public float MinimumPitch => _minimumPitch;
        public float MaximumPitch => Mathf.Max(_minimumPitch, _maximumPitch);
        public float PositionSmoothTime => _positionSmoothTime;
        public float RotationSharpness => _rotationSharpness;
    }

    public sealed class ThirdPersonCameraMode : IPlayerCameraMode, IPlayerCameraLookInput
    {
        private readonly ThirdPersonCameraProfile _profile;
        private readonly CameraCollisionResolver _collision;
        private PlayerCameraRig _rig;
        private Actor _actor;
        private Vector2 _pendingLook;
        private Vector3 _positionVelocity;
        private float _yaw;
        private float _pitch;

        public ThirdPersonCameraMode(ThirdPersonCameraProfile profile, CameraCollisionResolver collision = null)
        { _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile)); _collision = collision; }
        public PlayerCameraModeId Id => PlayerCameraModeId.ThirdPerson;
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public void Enter(PlayerCameraRig rig, Actor actor)
        {
            _rig = rig ?? throw new ArgumentNullException(nameof(rig)); _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _yaw = actor.transform.eulerAngles.y; _pendingLook = Vector2.zero; _positionVelocity = Vector3.zero;
            ApplyPose(0f, true);
        }
        public void Exit() { _rig = null; _actor = null; _pendingLook = Vector2.zero; }
        public void AddLookInput(Vector2 delta) => _pendingLook += delta;
        public void Tick(float deltaTime)
        {
            if (_rig == null || _actor == null) return;
            float sensitivity = _rig.Configuration.LookSensitivity;
            _yaw += _pendingLook.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - _pendingLook.y * sensitivity, _profile.MinimumPitch, _profile.MaximumPitch);
            _pendingLook = Vector2.zero; ApplyPose(Mathf.Max(0f, deltaTime), false);
        }

        private void ApplyPose(float deltaTime, bool immediate)
        {
            Vector3 pivot = _actor.transform.position + Vector3.up * _profile.PivotHeight;
            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = pivot - orbit * Vector3.forward * _profile.Distance;
            if (_collision != null) desired = _collision.Resolve(pivot, desired, _rig.Root.position, deltaTime);
            if (immediate || _profile.PositionSmoothTime <= 0f) _rig.Root.position = desired;
            else _rig.Root.position = Vector3.SmoothDamp(_rig.Root.position, desired, ref _positionVelocity,
                _profile.PositionSmoothTime, Mathf.Infinity, deltaTime);
            Quaternion look = Quaternion.LookRotation(pivot - _rig.Root.position, Vector3.up);
            if (immediate || _profile.RotationSharpness <= 0f) _rig.Root.rotation = look;
            else _rig.Root.rotation = Quaternion.Slerp(_rig.Root.rotation, look,
                1f - Mathf.Exp(-_profile.RotationSharpness * deltaTime));
        }
    }
}
