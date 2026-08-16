using System.Collections;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.PlayerRuntime;
using UnityEngine;
using UnityEngine.InputSystem;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;
using MyGameWorld.Client.EntityRuntime;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class ProceduralWorldPlayerCoordinator : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Vector2 _spawnCoordinates = Vector2.zero;
        [SerializeField, Min(10f)] private float _probeHeight = 600f;
        [SerializeField, Min(0.1f)] private float _groundClearance = 1.15f;
        [SerializeField] private UnityAssetCatalog _avatarAssetCatalog;
        [SerializeField] private AvatarPartCatalog _avatarPartCatalog;
        [SerializeField] private long _avatarSeed = 3201;
        [SerializeField] private RuntimeAnimatorController _humanoidMotionController;

        public PlayerRuntimeBootstrap PlayerRuntime { get; private set; }
        public Vector3 ResolvedSpawnPosition { get; private set; }
        public DevelopmentFreeCamera ReplacedDevelopmentCamera { get; private set; }

#if UNITY_EDITOR
        public void Configure(InputActionAsset inputActions, Vector2 spawnCoordinates,
            UnityAssetCatalog avatarAssetCatalog = null, AvatarPartCatalog avatarPartCatalog = null,
            RuntimeAnimatorController humanoidMotionController = null)
        {
            _inputActions = inputActions;
            _spawnCoordinates = spawnCoordinates;
            _avatarAssetCatalog = avatarAssetCatalog;
            _avatarPartCatalog = avatarPartCatalog;
            _humanoidMotionController = humanoidMotionController;
        }
#endif

        private IEnumerator Start()
        {
            ProceduralWorldSandbox sandbox = FindAnyObjectByType<ProceduralWorldSandbox>();
            if (sandbox == null)
            {
                Debug.LogError("[ProceduralPlayer] ProceduralWorldSandbox was not found.");
                yield break;
            }

            while (sandbox.Fingerprint == 0UL)
            {
                yield return null;
            }

            Physics.SyncTransforms();
            Vector3 origin = new Vector3(_spawnCoordinates.x, _probeHeight, _spawnCoordinates.y);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _probeHeight * 2f,
                WorldPhysicsLayers.GroundMask, QueryTriggerInteraction.Ignore))
            {
                Debug.LogError($"[ProceduralPlayer] No walkable terrain found below {_spawnCoordinates}.");
                yield break;
            }

            ResolvedSpawnPosition = hit.point + Vector3.up * _groundClearance;
            PlayerRuntime = gameObject.AddComponent<PlayerRuntimeBootstrap>();
            PlayerRuntime.SetAssembleOnAwake(false);
            PlayerRuntime.SetInputActions(_inputActions);
            PlayerRuntime.SetSpawnPosition(ResolvedSpawnPosition);
            PlayerRuntime.SetInitialCameraMode(PlayerCameraModeId.FirstPerson);
            PlayerRuntime.SetAvatarCatalogs(_avatarAssetCatalog, _avatarPartCatalog, _avatarSeed);
            EnvironmentalManager environment = FindAnyObjectByType<EnvironmentalManager>();
            WorldEnvironmentSnapshot snapshot = environment != null
                ? environment.Sample(ResolvedSpawnPosition, default)
                : default;
            PlayerRuntime.SetAvatarEnvironment(new AvatarEnvironmentContext(
                snapshot.BiomeId,
                snapshot.SurfaceId,
                Mathf.Max(0f, hit.point.y),
                1f - Mathf.Clamp01(hit.normal.y)));
            PlayerRuntime.SetHumanoidMotionController(_humanoidMotionController);
            DevelopmentFreeCamera developmentCamera = FindAnyObjectByType<DevelopmentFreeCamera>();
            if (developmentCamera != null)
            {
                ReplacedDevelopmentCamera = developmentCamera;
                PlayerRuntime.SetCameraToReplace(developmentCamera.GetComponent<Camera>());
            }

            PlayerRuntime.Assemble();
            Debug.Log($"[ProceduralPlayer] Spawned at {ResolvedSpawnPosition} using the shared Actor framework.");
        }
    }
}
