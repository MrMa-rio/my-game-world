using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class EnvironmentalManager : MonoBehaviour
    {
        [SerializeField] private WindProfile _wind = new WindProfile();
        [SerializeField] private WorldTimeProfile _time = new WorldTimeProfile();
        [SerializeField] private EnvironmentalBiomeKind _debugBiome = EnvironmentalBiomeKind.Grassland;
        [SerializeField, Range(0f, 2f)] private float _vfxDensity = 1f;
        [SerializeField, Min(1)] private int _physicalUpdatesPerFrame = 64;
        [SerializeField, Min(1)] private int _maximumVfxChunks = 12;
        [SerializeField] private bool _drawWindGizmos = true;
        [SerializeField] private BiomeEnvironmentalResponseProfile[] _biomeProfiles = System.Array.Empty<BiomeEnvironmentalResponseProfile>();
        private WindSystem _windSystem;
        private EnvironmentalPhysicalResponseSystem _physicalResponses;
        private EnvironmentalVfxSystem _vfx;
        private ProceduralWorldMaterialLibrary _materials;
        private WorldTimeSystem _timeSystem;
        private CelestialCycleSystem _celestialCycle;
        private CelestialEventSystem _celestialEvents;
        private bool _initialized;

        public WindSystem Wind => _windSystem;
        public EnvironmentalPhysicalResponseSystem PhysicalResponses => _physicalResponses;
        public int ActiveVfxChunks => _vfx != null ? _vfx.ActiveCellCount : 0;
        public EnvironmentalBiomeKind DebugBiome { get => _debugBiome; set => _debugBiome = value; }
        public float VfxDensity { get => _vfxDensity; set => _vfxDensity = Mathf.Clamp(value, 0f, 2f); }
        public WorldTimeSystem TimeSystem => _timeSystem;
        public int ActiveCelestialEvents => _celestialEvents != null ? _celestialEvents.ActiveCount : 0;
        public bool SpawnCelestialEvent(CelestialEventKind kind) => _celestialEvents != null && _celestialEvents.Spawn(kind, Camera.main);

        public void Initialize(ZoneGenerationResult zone, ProceduralWorldMaterialLibrary materials, ProceduralRuntimeManager runtime)
        {
            _materials = materials; _windSystem = new WindSystem(_wind, zone.DNA.Seed);
            _timeSystem = new WorldTimeSystem(_time); _celestialCycle = new CelestialCycleSystem(transform);
            _celestialEvents = new CelestialEventSystem(transform, zone.DNA.Seed);
            _physicalResponses = new EnvironmentalPhysicalResponseSystem(); runtime.SetEnvironmentalResponses(_physicalResponses);
            _vfx = new EnvironmentalVfxSystem(transform, materials.EnvironmentVfx, _maximumVfxChunks, _biomeProfiles); _vfx.Configure(zone);
            _windSystem.Tick(0f); _timeSystem.Tick(0f); _celestialCycle.Apply(_timeSystem.Snapshot); _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            _windSystem.Tick(Time.deltaTime);
            _timeSystem.Tick(Time.unscaledDeltaTime); _celestialCycle.Apply(_timeSystem.Snapshot);
            Camera camera = Camera.main;
            _celestialEvents.Tick(Time.unscaledDeltaTime, _timeSystem.Snapshot, camera);
            _physicalResponses.ProcessBatch(_windSystem, camera, _physicalUpdatesPerFrame);
            _vfx.Biome = _debugBiome; _vfx.DensityMultiplier = _vfxDensity;
            _vfx.Tick(Time.unscaledTime, camera, _windSystem);
        }

        private void OnDestroy() { _vfx?.Dispose(); _celestialEvents?.Dispose(); _celestialCycle?.Dispose(); }

        private void OnDrawGizmosSelected()
        {
            if (!_drawWindGizmos || _windSystem == null) return;
            Gizmos.color = Color.cyan;
            for (int z = -2; z <= 2; z++) for (int x = -2; x <= 2; x++)
            {
                Vector3 point = transform.position + new Vector3(x * 12f, 1f, z * 12f);
                WindSample sample = _windSystem.SampleWind(point);
                Gizmos.DrawRay(point, sample.Direction * (2f + sample.EffectiveStrength * 5f));
            }
        }
    }
}
