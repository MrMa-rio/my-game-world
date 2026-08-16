using System;
using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;
using MyGameWorld.Shared.Procedural;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Client.PlayerRuntime
{
    [DisallowMultipleComponent]
    public class PlayerRuntimeBootstrap : MonoBehaviour, IPlayerSensoryFeedback
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Vector3 _spawnPosition = new Vector3(0f, 2f, 0f);
        [SerializeField] private PlayerCameraModeId _initialCameraMode = PlayerCameraModeId.ThirdPerson;
        [SerializeField] private Camera _cameraToReplace;
        [SerializeField] private bool _assembleOnAwake = true;
        [Header("Procedural Avatar")]
        [SerializeField] private UnityAssetCatalog _avatarAssetCatalog;
        [SerializeField] private AvatarPartCatalog _avatarPartCatalog;
        [SerializeField] private long _avatarSeed = 3201;
        [SerializeField] private bool _masculineFrame = true;
        [SerializeField] private RuntimeAnimatorController _humanoidMotionController;
        private AvatarEnvironmentContext _avatarEnvironment;

        private readonly List<ScriptableObject> _runtimeProfiles = new List<ScriptableObject>();

        public Actor PlayerActor { get; private set; }
        public PlayerCameraSystem CameraSystem { get; private set; }
        public RuntimeAvatar Avatar { get; private set; }
        public ActorAnimationDriver AnimationDriver { get; private set; }
        public HumanoidMotionAnimation HumanoidMotion { get; private set; }
        public bool IsAssembled => PlayerActor != null;

        public void SetInputActions(InputActionAsset actions) => _inputActions = actions;

        public void SetSpawnPosition(Vector3 position) => _spawnPosition = position;

        public void SetInitialCameraMode(PlayerCameraModeId mode) => _initialCameraMode = mode;

        public void SetCameraToReplace(Camera camera) => _cameraToReplace = camera;

        public void SetAssembleOnAwake(bool assembleOnAwake) => _assembleOnAwake = assembleOnAwake;

        public void SetAvatarCatalogs(UnityAssetCatalog assetCatalog, AvatarPartCatalog partCatalog, long seed = 3201)
        {
            _avatarAssetCatalog = assetCatalog;
            _avatarPartCatalog = partCatalog;
            _avatarSeed = seed;
        }

        public void SetHumanoidMotionController(RuntimeAnimatorController controller)
            => _humanoidMotionController = controller;

        public void SetAvatarEnvironment(AvatarEnvironmentContext environment)
            => _avatarEnvironment = environment;

        protected virtual void Awake()
        {
            if (_assembleOnAwake && _inputActions != null)
            {
                Assemble();
            }
        }

        public void Assemble()
        {
            if (IsAssembled)
            {
                throw new InvalidOperationException("Player runtime is already assembled.");
            }

            if (_inputActions == null)
            {
                throw new InvalidOperationException("Player runtime requires an InputActionAsset.");
            }

            ActorLocomotionScheduler locomotionScheduler = gameObject.AddComponent<ActorLocomotionScheduler>();
            ActorSensorScheduler sensorScheduler = gameObject.AddComponent<ActorSensorScheduler>();
            GameObject player = new GameObject("Player Actor");
            player.transform.position = _spawnPosition;

            PlayerAssemblyRequest request = new PlayerAssemblyRequest
            {
                EntityId = new EntityId(3201),
                GlobalPosition = new GlobalPosition(_spawnPosition.x, _spawnPosition.y, _spawnPosition.z),
                CoordinateFrame = new WorldCoordinateFrame(new GlobalPosition()),
                EntityRegistry = new WorldEntityRegistry(),
                LocomotionScheduler = locomotionScheduler,
                SensorScheduler = sensorScheduler,
                SoundStream = new PerceptionSoundStream(),
                ScentField = new ScentField(),
                InputActions = _inputActions,
                Locomotion = CreateProfile<LocomotionProfile>(),
                Walk = CreateProfile<WalkProfile>(),
                Run = CreateProfile<RunProfile>(),
                Jump = CreateProfile<JumpProfile>(),
                PhysicalBody = CreateProfile<PhysicalBodyProfile>(),
                Vision = CreateProfile<VisionProfile>(),
                Hearing = CreateProfile<HearingProfile>(),
                Smell = CreateProfile<SmellProfile>()
            };
            PlayerActor = new PlayerActorAssembly().Assemble(player, request);
            Avatar = CreateAvatar(player.transform);
            Debug.Log($"[ProceduralAvatar] Seed {_avatarSeed}; family {Avatar.Style.Family}; " +
                $"visual scale {Avatar.Style.VisualScale}; head {Avatar.Style.HeadScale:0.00}; " +
                $"torso {Avatar.Style.TorsoWidth:0.00}; palette {Avatar.Style.ColorTint}.");
            IActorAnimationSink animationSink = CreateAnimationSink();
            AnimationDriver = player.AddComponent<ActorAnimationDriver>();
            AnimationDriver.Initialize(PlayerActor, CreateProfile<ActorAnimationDriverProfile>(), animationSink);

            if (_cameraToReplace != null)
            {
                _cameraToReplace.gameObject.SetActive(false);
            }

            GameObject cameraRoot = new GameObject("Player Camera Rig");
            Camera camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.tag = "MainCamera";
            cameraRoot.AddComponent<AudioListener>();
            CameraSystem = cameraRoot.AddComponent<PlayerCameraSystem>();
            CameraSystem.Initialize(PlayerActor, camera, CreateProfile<PlayerCameraConfiguration>());
            FirstPersonCameraMode first = new FirstPersonCameraMode(CreateProfile<FirstPersonCameraProfile>());
            CameraCollisionResolver collision = new CameraCollisionResolver(CreateProfile<CameraCollisionProfile>());
            ThirdPersonCameraMode third = new ThirdPersonCameraMode(CreateProfile<ThirdPersonCameraProfile>(), collision);
            CameraSystem.Modes.Register(first);
            CameraSystem.Modes.Register(third);
            CameraSystem.Modes.SetMode(_initialCameraMode);

            PlayerCameraLookBridge look = player.AddComponent<PlayerCameraLookBridge>();
            look.Configure(CameraSystem);
            PlayerActor.AddCapability<IPlayerCameraLookBridge>(look);
            PlayerCameraSwitchCapability switching = player.AddComponent<PlayerCameraSwitchCapability>();
            switching.Configure(CameraSystem);
            PlayerActor.AddCapability<IPlayerCameraSwitchCapability>(switching);
            PlayerHudView hudView = gameObject.AddComponent<PlayerHudView>();
            PlayerHudSystem hud = gameObject.AddComponent<PlayerHudSystem>();
            hud.Initialize(PlayerActor, CameraSystem, hudView);
            gameObject.AddComponent<PlayerWorldObserverSystem>().Initialize(PlayerActor, new WorldObserverRegistry());
            gameObject.AddComponent<PlayerSensoryPresentationSystem>().Initialize(PlayerActor, this);
            gameObject.AddComponent<ActorDebugView>().Initialize(PlayerActor, CameraSystem);
            gameObject.AddComponent<PlayerMouseCapture>();
            gameObject.AddComponent<PlayerAvatarCameraVisibility>().Initialize(Avatar, CameraSystem);
        }

        private RuntimeAvatar CreateAvatar(Transform parent)
        {
            if (_avatarAssetCatalog != null && _avatarPartCatalog != null)
            {
                AvatarCreationManager manager = gameObject.AddComponent<AvatarCreationManager>();
                manager.Initialize(new UnityAssetRegistry(_avatarAssetCatalog), _avatarPartCatalog.CreateDefinitions());
                AssetTrait family = _masculineFrame ? AssetTrait.MasculineFrame : AssetTrait.FeminineFrame;
                return manager.CreateImmediately(
                    _avatarSeed,
                    AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame | family,
                    parent,
                    _avatarEnvironment);
            }

            return ProceduralAvatarFallback.Create(parent, _avatarSeed);
        }

        private IActorAnimationSink CreateAnimationSink()
        {
            if (_humanoidMotionController != null)
            {
                HumanoidMotion = Avatar.gameObject.AddComponent<HumanoidMotionAnimation>();
                if (HumanoidMotion.Initialize(Avatar, _humanoidMotionController)) return HumanoidMotion;
                Destroy(HumanoidMotion);
                HumanoidMotion = null;
            }

            ProceduralAvatarAnimation procedural = Avatar.gameObject.AddComponent<ProceduralAvatarAnimation>();
            procedural.Initialize(Avatar);
            return procedural;
        }

        private T CreateProfile<T>() where T : ScriptableObject
        {
            T profile = ScriptableObject.CreateInstance<T>();
            _runtimeProfiles.Add(profile);
            return profile;
        }

        protected virtual void OnDestroy()
        {
            for (int index = 0; index < _runtimeProfiles.Count; index++)
            {
                if (_runtimeProfiles[index] != null)
                {
                    Destroy(_runtimeProfiles[index]);
                }
            }
        }

        public void PresentVision(IReadOnlyList<IVisionTarget> targets) { }
        public void PresentHearing(HeardSound sound) { }
        public void PresentTouch(TouchEvent contact) { }
        public void PresentSmell(IReadOnlyList<DetectedScent> scents) { }
        public void PresentTaste(TasteStimulus taste) { }
        public void PresentProprioception(ProprioceptionSnapshot state) { }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerMouseCapture : MonoBehaviour
    {
        private void OnEnable() => SetCaptured(true);

        private void OnDisable() => SetCaptured(false);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                SetCaptured(Cursor.lockState != CursorLockMode.Locked);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCaptured(true);
            }
        }

        private static void SetCaptured(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
