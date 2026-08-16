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
        [SerializeField] private Vector3 _shoulderOffset = new Vector3(0.45f, 0.15f, 0f);
        [SerializeField, Min(0f)] private float _verticalArmLength = 0.3f;
        [SerializeField, Range(-80f, 0f)] private float _minimumPitch = -35f;
        [SerializeField, Range(0f, 80f)] private float _maximumPitch = 65f;
        [SerializeField, Min(0f)] private float _trackingDamping = 0.08f;
        public float Distance => _distance;
        public float PivotHeight => _pivotHeight;
        public Vector3 ShoulderOffset => _shoulderOffset;
        public float VerticalArmLength => _verticalArmLength;
        public float MinimumPitch => _minimumPitch;
        public float MaximumPitch => Mathf.Max(_minimumPitch, _maximumPitch);
        public float TrackingDamping => _trackingDamping;
    }

    public sealed class ThirdPersonCameraMode : IPlayerCameraMode, IPlayerCameraLookInput
    {
        private readonly ThirdPersonCameraProfile _profile;
        private readonly CameraCollisionResolver _collision;
        private PlayerCameraRig _rig;
        private Actor _actor;
        private Vector2 _pendingLook;
        private Vector3 _trackedOrigin;
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
            _yaw = actor.transform.eulerAngles.y; _pendingLook = Vector2.zero;
            _trackedOrigin = ResolveTargetOrigin();
            _collision?.Reset(_profile.Distance);
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
            _actor.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _pendingLook = Vector2.zero; ApplyPose(Mathf.Max(0f, deltaTime), false);
        }

        private void ApplyPose(float deltaTime, bool immediate)
        {
            Vector3 targetOrigin = ResolveTargetOrigin();
            if (immediate || _profile.TrackingDamping <= 0f) _trackedOrigin = targetOrigin;
            else _trackedOrigin = Vector3.Lerp(_trackedOrigin, targetOrigin,
                1f - Mathf.Exp(-deltaTime / Mathf.Max(0.0001f, _profile.TrackingDamping)));

            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 shoulder = _trackedOrigin + yawRotation * _profile.ShoulderOffset;
            Vector3 hand = shoulder + orbit * Vector3.up * _profile.VerticalArmLength;
            Vector3 desired = hand - orbit * Vector3.forward * _profile.Distance;
            if (_collision != null) desired = _collision.Resolve(_trackedOrigin, desired, deltaTime, _actor.transform);
            _rig.Root.SetPositionAndRotation(desired, orbit);
        }

        private Vector3 ResolveTargetOrigin()
            => _actor.transform.position + Vector3.up * _profile.PivotHeight;
    }
}
