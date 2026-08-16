using System;
using MyGameWorld.Client.ActorRuntime;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Player/First Person Camera Profile")]
    public sealed class FirstPersonCameraProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _eyeHeight = 1.65f;
        [SerializeField, Range(-89f, 0f)] private float _minimumPitch = -80f;
        [SerializeField, Range(0f, 89f)] private float _maximumPitch = 80f;
        public float EyeHeight => _eyeHeight;
        public float MinimumPitch => _minimumPitch;
        public float MaximumPitch => Mathf.Max(_minimumPitch, _maximumPitch);
    }

    public sealed class FirstPersonCameraMode : IPlayerCameraMode, IPlayerCameraLookInput
    {
        private readonly FirstPersonCameraProfile _profile;
        private PlayerCameraRig _rig;
        private Actor _actor;
        private Vector2 _pendingLook;
        private float _pitch;

        public FirstPersonCameraMode(FirstPersonCameraProfile profile)
            => _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        public PlayerCameraModeId Id => PlayerCameraModeId.FirstPerson;
        public float Pitch => _pitch;

        public void Enter(PlayerCameraRig rig, Actor actor)
        {
            _rig = rig ?? throw new ArgumentNullException(nameof(rig));
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _pendingLook = Vector2.zero;
            ApplyPose();
        }

        public void Exit() { _pendingLook = Vector2.zero; _rig = null; _actor = null; }
        public void AddLookInput(Vector2 delta) => _pendingLook += delta;

        public void Tick(float deltaTime)
        {
            if (_rig == null || _actor == null) return;
            float sensitivity = _rig.Configuration.LookSensitivity;
            float yaw = _pendingLook.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - _pendingLook.y * sensitivity, _profile.MinimumPitch, _profile.MaximumPitch);
            _actor.transform.Rotate(Vector3.up, yaw, Space.World);
            _pendingLook = Vector2.zero;
            ApplyPose();
        }

        private void ApplyPose()
        {
            Transform actorTransform = _actor.transform;
            _rig.Root.position = actorTransform.position + Vector3.up * _profile.EyeHeight;
            _rig.Root.rotation = actorTransform.rotation * Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
