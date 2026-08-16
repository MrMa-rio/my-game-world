using System;
using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    public enum PlayerCameraModeId : byte { FirstPerson = 1, ThirdPerson = 2 }

    public interface IPlayerCameraMode
    {
        PlayerCameraModeId Id { get; }
        void Enter(PlayerCameraRig rig, Actor actor);
        void Exit();
        void Tick(float deltaTime);
    }

    public interface IPlayerCameraLookInput
    {
        void AddLookInput(Vector2 delta);
    }

    [CreateAssetMenu(menuName = "My Game World/Player/Camera Configuration")]
    public sealed class PlayerCameraConfiguration : ScriptableObject
    {
        [SerializeField, Min(0.001f)] private float _nearClipPlane = 0.05f;
        [SerializeField, Min(0.01f)] private float _lookSensitivity = 0.12f;
        public float NearClipPlane => _nearClipPlane;
        public float LookSensitivity => _lookSensitivity;
    }

    public sealed class PlayerCameraRig
    {
        public PlayerCameraRig(Camera camera, Transform root, PlayerCameraConfiguration configuration)
        {
            Camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Configuration = configuration != null ? configuration : throw new ArgumentNullException(nameof(configuration));
            Camera.nearClipPlane = configuration.NearClipPlane;
        }
        public Camera Camera { get; }
        public Transform Root { get; }
        public PlayerCameraConfiguration Configuration { get; }
    }

    public sealed class PlayerCameraModeController
    {
        private readonly Dictionary<PlayerCameraModeId, IPlayerCameraMode> _modes = new Dictionary<PlayerCameraModeId, IPlayerCameraMode>();
        private readonly PlayerCameraRig _rig;
        private readonly Actor _actor;
        public PlayerCameraModeController(PlayerCameraRig rig, Actor actor) { _rig = rig; _actor = actor; }
        public IPlayerCameraMode ActiveMode { get; private set; }
        public event Action<PlayerCameraModeId> ModeChanged;
        public void Register(IPlayerCameraMode mode)
        {
            if (mode == null) throw new ArgumentNullException(nameof(mode));
            if (_modes.ContainsKey(mode.Id)) throw new InvalidOperationException($"Camera mode {mode.Id} is already registered.");
            _modes.Add(mode.Id, mode);
        }
        public bool SetMode(PlayerCameraModeId id)
        {
            if (!_modes.TryGetValue(id, out IPlayerCameraMode next)) return false;
            if (ReferenceEquals(next, ActiveMode)) return true;
            ActiveMode?.Exit(); ActiveMode = next; ActiveMode.Enter(_rig, _actor); ModeChanged?.Invoke(id); return true;
        }
        public void Tick(float deltaTime) => ActiveMode?.Tick(deltaTime);
    }

    [DisallowMultipleComponent]
    public sealed class PlayerCameraSystem : MonoBehaviour
    {
        public PlayerCameraRig Rig { get; private set; }
        public PlayerCameraModeController Modes { get; private set; }
        public bool IsInitialized => Rig != null;
        public bool IsThirdPersonActive => Modes?.ActiveMode?.Id == PlayerCameraModeId.ThirdPerson;
        public void Initialize(Actor actor, Camera camera, PlayerCameraConfiguration configuration)
        {
            if (IsInitialized) throw new InvalidOperationException("Player camera is already initialized.");
            if (actor == null || !actor.IsInitialized) throw new InvalidOperationException("Player camera requires an initialized Actor.");
            Rig = new PlayerCameraRig(camera, transform, configuration);
            Modes = new PlayerCameraModeController(Rig, actor);
        }
        public bool SubmitLook(Vector2 delta)
        {
            if (!IsInitialized || !(Modes.ActiveMode is IPlayerCameraLookInput receiver)) return false;
            receiver.AddLookInput(delta); return true;
        }
        private void LateUpdate() { if (IsInitialized) Modes.Tick(Time.deltaTime); }
    }

    public interface IPlayerCameraLookBridge : IActorCapability { }

    [DisallowMultipleComponent]
    public sealed class PlayerCameraLookBridge : ActorCapability, IPlayerCameraLookBridge, IActorIntentHandler<LookIntent>
    {
        private PlayerCameraSystem _cameraSystem;
        public void Configure(PlayerCameraSystem cameraSystem)
        {
            if (IsInitialized) throw new InvalidOperationException("Camera look bridge cannot change after initialization.");
            _cameraSystem = cameraSystem != null ? cameraSystem : throw new ArgumentNullException(nameof(cameraSystem));
        }
        protected override void OnInitialized()
        {
            if (_cameraSystem == null || !_cameraSystem.IsInitialized)
                throw new InvalidOperationException("Camera look bridge requires an initialized PlayerCameraSystem.");
            RegisterIntentHandler<LookIntent>(this);
        }
        public void HandleIntent(in LookIntent intent) => _cameraSystem.SubmitLook(intent.Delta);
    }

    public interface IPlayerCameraSwitchCapability : IActorCapability { }

    [DisallowMultipleComponent]
    public sealed class PlayerCameraSwitchCapability : ActorCapability, IPlayerCameraSwitchCapability,
        IActorIntentHandler<ChangeCameraIntent>
    {
        private PlayerCameraSystem _cameraSystem;
        public void Configure(PlayerCameraSystem cameraSystem)
        {
            if (IsInitialized) throw new InvalidOperationException("Camera switch cannot change after initialization.");
            _cameraSystem = cameraSystem != null ? cameraSystem : throw new ArgumentNullException(nameof(cameraSystem));
        }
        protected override void OnInitialized()
        {
            if (_cameraSystem == null || !_cameraSystem.IsInitialized)
                throw new InvalidOperationException("Camera switch requires an initialized PlayerCameraSystem.");
            RegisterIntentHandler<ChangeCameraIntent>(this);
        }
        public void HandleIntent(in ChangeCameraIntent intent)
        {
            PlayerCameraModeId next = _cameraSystem.Modes.ActiveMode?.Id == PlayerCameraModeId.FirstPerson
                ? PlayerCameraModeId.ThirdPerson : PlayerCameraModeId.FirstPerson;
            _cameraSystem.Modes.SetMode(next);
        }
    }
}
