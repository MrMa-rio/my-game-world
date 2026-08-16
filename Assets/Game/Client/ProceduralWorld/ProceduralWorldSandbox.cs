using System;
using System.Collections.Generic;
using System.Diagnostics;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class ProceduralWorldSandbox : MonoBehaviour
    {
        [Header("Zone DNA")]
        [SerializeField, Min(1)]
        private long _zoneId = 10;

        [SerializeField]
        private long _zoneSeed = 48151623;

        [Header("Terrain Geometry")]
        [SerializeField, Min(10f)]
        private float _width = 1000f;

        [SerializeField, Min(10f)]
        private float _depth = 1000f;

        [SerializeField, Min(2)]
        private int _requestedResolution = 257;

        [SerializeField, Min(1f)]
        private float _maxHeight = 420f;

        [SerializeField, Min(2)]
        private int _targetTriangleBudget = 80000;

        [SerializeField, Min(1)]
        private int _chunkCountPerAxis = 10;

        [SerializeField]
        private TerrainShadingMode _shadingMode = TerrainShadingMode.Flat;

        [Header("Runtime")]
        [SerializeField]
        private bool _generateOnStart = true;

        private readonly List<UnityTerrainChunkRuntime> _terrainChunks = new List<UnityTerrainChunkRuntime>();
        private readonly List<UnityLiquidBodyRuntime> _liquidBodies = new List<UnityLiquidBodyRuntime>();
        private GameObject _runtimeRoot;
        private ProceduralWorldMaterialLibrary _materials;
        private ProceduralRuntimeManager _runtimeManager;
        private EnvironmentalManager _environmentalManager;
        private DistantWorldRenderer _distantWorld;
        private ZoneGenerationResult _result;
        private bool _wireframeVisible;

        public long ZoneId => _zoneId;

        public long ZoneSeed => _zoneSeed;

        public ushort GeneratorVersion => TerrainGeneratorV6.GeneratorVersion.Value;

        public ulong Fingerprint => _result != null ? _result.Fingerprint : 0UL;

        public float GenerationMilliseconds { get; private set; }

        public float TerrainWidth => _result != null ? _result.Terrain.Config.Width : _width;

        public float TerrainDepth => _result != null ? _result.Terrain.Config.Depth : _depth;

        public int ResolvedResolution => _result != null ? _result.Terrain.Config.ResolvedResolution : 0;

        public int LogicalVertexCount => _result != null ? _result.Terrain.LogicalVertexCount : 0;

        public int RenderedVertexCount => _result != null ? _result.Terrain.RenderedVertexCount : 0;

        public int TriangleCount => _result != null ? _result.Terrain.TriangleCount : 0;

        public int TriangleBudget => _targetTriangleBudget;

        public int ChunkCount => _result != null ? _result.Terrain.Chunks.Count : 0;

        public int DecorationCount => _runtimeManager != null ? _runtimeManager.ActiveCount : 0;
        public int PlannedDecorationCount => _result != null ? _result.Decorations.Count : 0;
        public int SingularTerrainFeatureCount => _result != null ? _result.Features.ElementCount : 0;

        public int TreeCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.Tree) : 0;

        public int RockCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.Rock) : 0;

        public int BushCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.Bush) : 0;

        public int FlowerCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.Flower) : 0;
        public int FlowerClusterCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.FlowerCluster) : 0;
        public int MushroomCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.Mushroom) : 0;
        public int MushroomClusterCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.MushroomCluster) : 0;
        public int TreeClusterCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.TreeCluster) : 0;
        public int RockClusterCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.RockCluster) : 0;
        public int BushClusterCount => _runtimeManager != null ? _runtimeManager.Count(DecorationKind.BushCluster) : 0;

        public ProceduralRuntimeMetrics RuntimeMetrics => _runtimeManager != null ? _runtimeManager.Metrics : default;
        public WindSample Wind => _environmentalManager?.Wind != null ? _environmentalManager.Wind.GlobalSample : default;
        public int ActiveEnvironmentalVfxChunks => _environmentalManager != null ? _environmentalManager.ActiveVfxChunks : 0;
        public EnvironmentalBiomeKind DebugEnvironmentalBiome => _environmentalManager != null ? _environmentalManager.DebugBiome : EnvironmentalBiomeKind.Grassland;
        public WorldTimeSnapshot WorldTime => _environmentalManager?.TimeSystem != null ? _environmentalManager.TimeSystem.Snapshot : default;
        public int ActiveCelestialEvents => _environmentalManager != null ? _environmentalManager.ActiveCelestialEvents : 0;
        public int ProceduralStarCount => _environmentalManager != null ? _environmentalManager.ProceduralStarCount : 0;
        public IReadOnlyList<ProceduralStar> ProceduralStars => _environmentalManager != null ? _environmentalManager.ProceduralStars : Array.Empty<ProceduralStar>();
        public float LocalCelestialLuminosity => _environmentalManager != null ? _environmentalManager.LocalCelestialLuminosity : 1f;
        public float StarDensityMultiplier => _environmentalManager != null ? _environmentalManager.StarDensityMultiplier : 1f;
        public int EstimatedVisibleStars => _environmentalManager != null ? _environmentalManager.EstimatedVisibleStars : 0;
        public float NebulaVisibility => _environmentalManager != null ? _environmentalManager.NebulaVisibility : 0f;
        public ProceduralShaderQuality ShaderQuality => _environmentalManager != null ? _environmentalManager.ShaderQuality : ProceduralShaderQuality.Low;
        public ProceduralShaderBudget ShaderBudget => _environmentalManager != null ? _environmentalManager.ShaderBudget : default;
        public DistantWorldMetrics DistantMetrics => _distantWorld != null ? _distantWorld.Metrics : default;

        public bool IsHudVisible { get; private set; } = true;

        private void Awake()
        {
            _materials = new ProceduralWorldMaterialLibrary();
            _runtimeManager = gameObject.AddComponent<ProceduralRuntimeManager>();
            _runtimeManager.Initialize(_materials);
        }

        private void Start()
        {
            if (_generateOnStart)
            {
                Generate();
            }
        }

        private void OnDestroy()
        {
            DestroyRuntime(releaseDecorations: false);
            _materials?.Dispose();
        }

        public void ToggleHud() => IsHudVisible = !IsHudVisible;

        public void ToggleWireframe()
        {
            _wireframeVisible = !_wireframeVisible;
            for (int index = 0; index < _terrainChunks.Count; index++)
            {
                _terrainChunks[index].SetWireframeVisible(_wireframeVisible);
            }
        }

        public void RegenerateSameSeed()
        {
            ulong previous = Fingerprint;
            Generate();
            bool matches = previous == 0UL || previous == Fingerprint;
            Debug.Log($"Regenerated seed {_zoneSeed}. Fingerprint {(matches ? "MATCH" : "MISMATCH")}: {Fingerprint:X16}");
        }

        public void GenerateNextSeed()
        {
            _zoneSeed = unchecked(_zoneSeed + 1);
            Generate();
            Debug.Log($"Generated next seed {_zoneSeed}. Fingerprint: {Fingerprint:X16}");
        }

        public void CycleWindStrength()
        {
            if (_environmentalManager?.Wind == null) return;
            float current = _environmentalManager.Wind.Profile.Strength;
            _environmentalManager.Wind.Profile.Strength = current < 0.3f ? 0.5f : current < 0.7f ? 0.9f : 0.15f;
        }

        public void CycleEnvironmentalBiome()
        {
            if (_environmentalManager == null) return;
            int next = ((int)_environmentalManager.DebugBiome % 4) + 1;
            _environmentalManager.DebugBiome = (EnvironmentalBiomeKind)next;
        }

        public void CycleVfxDensity()
        {
            if (_environmentalManager == null) return;
            float current = _environmentalManager.VfxDensity;
            _environmentalManager.VfxDensity = current < 0.75f ? 1f : current < 1.5f ? 2f : 0.25f;
        }

        public void AdvanceWorldTime() => _environmentalManager?.TimeSystem?.AdvanceHours(3f);
        public void SetWorldHour(float hour) => _environmentalManager?.TimeSystem?.SetHour(hour);
        public void ToggleWorldTimePause()
        {
            if (_environmentalManager?.TimeSystem == null) return;
            _environmentalManager.TimeSystem.Profile.Paused = !_environmentalManager.TimeSystem.Profile.Paused;
        }
        public void SpawnShootingStar() => _environmentalManager?.SpawnCelestialEvent(CelestialEventKind.ShootingStar);
        public void SpawnMeteor() => _environmentalManager?.SpawnCelestialEvent(CelestialEventKind.Meteor);
        public void CycleShaderQuality() => _environmentalManager?.CycleShaderQuality();
        public void ToggleDistantLodDebug() { if (_distantWorld != null) _distantWorld.DebugLodColors = !_distantWorld.DebugLodColors; }
        public void ToggleDistantQuadtreeDebug() { if (_distantWorld != null) _distantWorld.DrawQuadtree = !_distantWorld.DrawQuadtree; }
        public void ToggleMaximumVisibility() { if (_distantWorld != null) _distantWorld.MaximumVisibility = !_distantWorld.MaximumVisibility; }

        public void Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            TerrainGenerationConfig config = new TerrainGenerationConfig(
                _width,
                _depth,
                _requestedResolution,
                _maxHeight,
                _targetTriangleBudget,
                _chunkCountPerAxis,
                _chunkCountPerAxis,
                _shadingMode);
            TerrainScalabilityPolicy.EnsureDetailedSupported(config);
            BiomeDefinition biome = BiomeDefinition.CreateExpandedTemperateGrassland();
            LargeScaleTerrainProfile largeScale = LargeScaleTerrainProfile.CreateGeologicalHighlands();
            ZoneDNA dna = new ZoneDNA(
                new ZoneId(_zoneId),
                _zoneSeed,
                BiomeId.TemperateGrassland,
                TerrainProfileId.RollingLowPoly,
                TerrainGeneratorV6.GeneratorVersion,
                new AssetCatalogVersion(3));
            ZoneGeneratorV6 generator = new ZoneGeneratorV6(config, biome, largeScale, WorldGenerationLimits.ScalableHighlands);
            ZoneGenerationResult nextResult = generator.Generate(dna);

            DestroyRuntime();
            _runtimeRoot = new GameObject($"Zone TEST_{_zoneId:000} Runtime");
            _runtimeRoot.transform.SetParent(transform, false);
            _runtimeManager.SetInstanceParent(_runtimeRoot.transform);
            _environmentalManager = _runtimeRoot.AddComponent<EnvironmentalManager>();
            _environmentalManager.Initialize(nextResult, _materials, _runtimeManager);
            _distantWorld = _runtimeRoot.AddComponent<DistantWorldRenderer>();
            HeightFieldGeneratorV2 sharedHeightSource = new HeightFieldGeneratorV2(nextResult.DNA, config, biome, nextResult.Features, largeScale);
            _distantWorld.Initialize(sharedHeightSource, biome, config, _zoneSeed, TerrainGeneratorV6.GeneratorVersion.Value,
                _materials.Terrain, Camera.main != null ? Camera.main.transform : null);
            for (int index = 0; index < nextResult.Terrain.Chunks.Count; index++)
            {
                UnityTerrainChunkRuntime chunk = _runtimeManager.MaterializeTerrainChunk(
                    nextResult.Terrain.Chunks[index],
                    nextResult.Features.Terrain,
                    _materials.Wireframe);
                chunk.SetWireframeVisible(_wireframeVisible);
                _terrainChunks.Add(chunk);
            }

            Vector3 viewer = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            for (int index = 0; index < nextResult.Decorations.Count; index++)
            {
                DecorationPlacement definition = nextResult.Decorations[index];
                WorldVector3 normal = nextResult.Terrain.HeightField.SampleNormal(definition.Position.X, definition.Position.Z);
                Vector3 unityNormal = new Vector3(normal.X, normal.Y, normal.Z);
                ProceduralEnvironmentContext environment = new ProceduralEnvironmentContext(
                    nextResult.DNA.BiomeId,
                    unityNormal,
                    1f - normal.Y,
                    definition.Position.Y);
                ProceduralVisualLod lod = _runtimeManager.ResolveLod(definition, viewer);
                float distance = Vector3.Distance(viewer, new Vector3(definition.Position.X, definition.Position.Y, definition.Position.Z));
                GenerationPriority priority = distance < 28f ? GenerationPriority.High : distance < 58f ? GenerationPriority.Normal : GenerationPriority.Low;
                _runtimeManager.Request(new ProceduralGenerationRequest(definition, environment, lod, priority));
            }
            _result = nextResult;
            for (int index = 0; index < nextResult.Features.Liquids.Count; index++)
            {
                LiquidBodyDNA liquid = nextResult.Features.Liquids[index];
                Material material = liquid.Substance == LiquidSubstance.Lava ? _materials.Lava : _materials.Water;
                _liquidBodies.Add(new UnityLiquidBodyRuntime(liquid, _runtimeRoot.transform, material));
            }
            stopwatch.Stop();
            GenerationMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
            Debug.Log(
                $"Generated TEST_{_zoneId:000} seed {_zoneSeed} in {GenerationMilliseconds:0.0} ms. " +
                $"Logical vertices: {LogicalVertexCount}, rendered vertices: {RenderedVertexCount}, " +
                $"triangles: {TriangleCount}/{TriangleBudget}, queued objects: {nextResult.Decorations.Count}, fingerprint: {Fingerprint:X16}.");
        }

        public IReadOnlyList<WorldElementDNA> ResolveTerrainContact(Vector3 worldPosition)
        {
            return _result != null
                ? _result.ResolveTerrainContact(worldPosition.x, worldPosition.z)
                : Array.Empty<WorldElementDNA>();
        }

        private void DestroyRuntime(bool releaseDecorations = true)
        {
            if (releaseDecorations) _runtimeManager?.ReleaseAll();
            for (int index = 0; index < _terrainChunks.Count; index++)
            {
                _terrainChunks[index].Dispose();
            }

            _terrainChunks.Clear();
            for (int index = 0; index < _liquidBodies.Count; index++) _liquidBodies[index].Dispose();
            _liquidBodies.Clear();
            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot);
                _runtimeRoot = null;
                _environmentalManager = null;
                _distantWorld = null;
            }

            _result = null;
        }
    }
}
