using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Client.PlayerRuntime
{
    [DisallowMultipleComponent]
    public sealed class PlayerTestSceneBootstrap : MonoBehaviour, IPlayerSensoryFeedback
    {
        [SerializeField] private InputActionAsset _inputActions;
        private readonly List<ScriptableObject> _runtimeProfiles = new List<ScriptableObject>();
        public Actor PlayerActor { get; private set; }
        public PlayerCameraSystem CameraSystem { get; private set; }
        public void SetInputActions(InputActionAsset actions) => _inputActions = actions;

        private void Awake()
        {
            if (_inputActions == null) return;
            ActorLocomotionScheduler locomotionScheduler = gameObject.AddComponent<ActorLocomotionScheduler>();
            ActorSensorScheduler sensorScheduler = gameObject.AddComponent<ActorSensorScheduler>();
            GameObject player = new GameObject("Technical Player"); player.transform.position = new Vector3(0f, 2f, 0f);
            GameObject bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule); bodyVisual.name = "Player Scale Reference";
            Destroy(bodyVisual.GetComponent<Collider>()); bodyVisual.transform.SetParent(player.transform, false);
            PlayerAssemblyRequest request = new PlayerAssemblyRequest
            {
                EntityId = new EntityId(3201), GlobalPosition = new GlobalPosition(0d, 2d, 0d),
                CoordinateFrame = new WorldCoordinateFrame(new GlobalPosition()), EntityRegistry = new WorldEntityRegistry(),
                LocomotionScheduler = locomotionScheduler, SensorScheduler = sensorScheduler,
                SoundStream = new PerceptionSoundStream(), ScentField = new ScentField(), InputActions = _inputActions,
                Locomotion = CreateProfile<LocomotionProfile>(), Walk = CreateProfile<WalkProfile>(), Run = CreateProfile<RunProfile>(),
                Jump = CreateProfile<JumpProfile>(), PhysicalBody = CreateProfile<PhysicalBodyProfile>(),
                Vision = CreateProfile<VisionProfile>(), Hearing = CreateProfile<HearingProfile>(), Smell = CreateProfile<SmellProfile>()
            };
            PlayerActor = new PlayerActorAssembly().Assemble(player, request);

            GameObject cameraRoot = new GameObject("Player Camera Rig"); Camera camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.tag = "MainCamera"; cameraRoot.AddComponent<AudioListener>(); CameraSystem = cameraRoot.AddComponent<PlayerCameraSystem>();
            CameraSystem.Initialize(PlayerActor, camera, CreateProfile<PlayerCameraConfiguration>());
            FirstPersonCameraMode first = new FirstPersonCameraMode(CreateProfile<FirstPersonCameraProfile>());
            CameraCollisionResolver collision = new CameraCollisionResolver(CreateProfile<CameraCollisionProfile>());
            ThirdPersonCameraMode third = new ThirdPersonCameraMode(CreateProfile<ThirdPersonCameraProfile>(), collision);
            CameraSystem.Modes.Register(first); CameraSystem.Modes.Register(third); CameraSystem.Modes.SetMode(PlayerCameraModeId.ThirdPerson);
            PlayerCameraLookBridge look = player.AddComponent<PlayerCameraLookBridge>(); look.Configure(CameraSystem); PlayerActor.AddCapability<IPlayerCameraLookBridge>(look);
            PlayerCameraSwitchCapability switching = player.AddComponent<PlayerCameraSwitchCapability>(); switching.Configure(CameraSystem);
            PlayerActor.AddCapability<IPlayerCameraSwitchCapability>(switching);
            PlayerHudView hudView = gameObject.AddComponent<PlayerHudView>(); PlayerHudSystem hud = gameObject.AddComponent<PlayerHudSystem>();
            hud.Initialize(PlayerActor, CameraSystem, hudView); gameObject.AddComponent<PlayerWorldObserverSystem>().Initialize(PlayerActor, new WorldObserverRegistry());
            gameObject.AddComponent<PlayerSensoryPresentationSystem>().Initialize(PlayerActor, this);
            gameObject.AddComponent<ActorDebugView>().Initialize(PlayerActor, CameraSystem);
        }

        private T CreateProfile<T>() where T : ScriptableObject
        { T profile = ScriptableObject.CreateInstance<T>(); _runtimeProfiles.Add(profile); return profile; }
        private void OnDestroy()
        { for (int index = 0; index < _runtimeProfiles.Count; index++) if (_runtimeProfiles[index] != null) Destroy(_runtimeProfiles[index]); }
        public void PresentVision(IReadOnlyList<IVisionTarget> targets) { }
        public void PresentHearing(HeardSound sound) { }
        public void PresentTouch(TouchEvent contact) { }
        public void PresentSmell(IReadOnlyList<DetectedScent> scents) { }
        public void PresentTaste(TasteStimulus taste) { }
        public void PresentProprioception(ProprioceptionSnapshot state) { }
    }
}
