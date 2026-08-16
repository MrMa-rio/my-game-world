using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MyGameWorld.Shared.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class DistantWorldRenderer : MonoBehaviour
    {
        [SerializeField] private Transform _viewer;
        [SerializeField] private bool _debugLodColors;
        [SerializeField] private bool _drawQuadtree;
        [SerializeField] private bool _maximumVisibility;
        [SerializeField, Min(1000f)] private float _atmosphericVisibilityDistance = 40000f;
        [SerializeField, Min(1000f)] private float _terrainRenderDistance = 50000f;
        [SerializeField, Min(0.1f)] private float _cpuBudgetMilliseconds = 2f;
        [SerializeField, Min(1)] private int _meshCommitsPerFrame = 2;
        [SerializeField, Min(1000f)] private float _rebaseThreshold = 8000f;

        private readonly Dictionary<WorldCellCoordinate, CellRuntime> _active = new Dictionary<WorldCellCoordinate, CellRuntime>();
        private readonly Queue<CellRuntime> _pool = new Queue<CellRuntime>();
        private readonly Queue<CompletedBuild> _completed = new Queue<CompletedBuild>();
        private readonly HashSet<WorldCellCoordinate> _pending = new HashSet<WorldCellCoordinate>();
        private readonly object _completionLock = new object();
        private WorldRepresentationProfile _profile;
        private WorldSpatialHierarchy _hierarchy;
        private HierarchicalWorldGenerator _generator;
        private TerrainRepresentationBuilder _builder;
        private WorldStreamingBudget _budget;
        private Material _material;
        private GlobalPosition _origin;
        private GlobalPosition _viewerGlobal;
        private long _frame;
        private float _nextSelectionTime;
        private bool _initialized;
        private DistantWorldMetrics _metrics;
        private WorldBounds? _detailedWorldBounds;
        private float _stabilityLodBias = 1f;
        private float _subpixelThreshold = 1.5f;
        private bool _stabilizeDistantWorld = true;
        private bool _temporalStability;

        public event Action<GlobalPosition, GlobalPosition> WorldRebased;
        public DistantWorldMetrics Metrics => _metrics;
        public GlobalPosition ViewerGlobalPosition => _viewerGlobal;
        public float AtmosphericVisibilityDistance { get => _atmosphericVisibilityDistance; set => _atmosphericVisibilityDistance = Mathf.Max(1000f, value); }
        public bool MaximumVisibility { get => _maximumVisibility; set => _maximumVisibility = value; }
        public bool DebugLodColors { get => _debugLodColors; set { _debugLodColors = value; RefreshExistingMaterials(); } }
        public bool DrawQuadtree { get => _drawQuadtree; set => _drawQuadtree = value; }
        public void ApplyImageStability(float lodBias, float subpixelThreshold, bool stabilizeDistantWorld, bool temporalStability)
        {
            _stabilityLodBias = Mathf.Max(0.5f, lodBias); _subpixelThreshold = Mathf.Max(0.5f, subpixelThreshold);
            _stabilizeDistantWorld = stabilizeDistantWorld; _temporalStability = temporalStability;
            Shader.SetGlobalVector("_DistantWorldStability", new Vector4(_stabilityLodBias, _subpixelThreshold,
                _stabilizeDistantWorld ? 1f : 0f, _temporalStability ? 1f : 0f));
        }

        public void Initialize(long seed, ushort generationVersion, Material terrainMaterial, Transform viewer = null)
        {
            DisposeCells();
            _viewer = viewer != null ? viewer : Camera.main != null ? Camera.main.transform : transform;
            Camera viewerCamera = _viewer.GetComponent<Camera>();
            if (viewerCamera != null) viewerCamera.farClipPlane = Mathf.Max(viewerCamera.farClipPlane, _terrainRenderDistance * 1.05f);
            _profile = WorldRepresentationProfile.CreateDefault();
            _hierarchy = new WorldSpatialHierarchy();
            _generator = new HierarchicalWorldGenerator(seed, generationVersion);
            _builder = new TerrainRepresentationBuilder();
            _budget = new WorldStreamingBudget(_cpuBudgetMilliseconds, _meshCommitsPerFrame, 4, 2);
            _material = terrainMaterial;
            _origin = new GlobalPosition(0d, 0d, 0d);
            _initialized = true;
            RefreshSelection(force: true);
        }

        public void Initialize(HeightFieldGeneratorV2 sharedHeightSource, BiomeDefinition biome,
            TerrainGenerationConfig config, long worldSeed, ushort generationVersion, Material terrainMaterial, Transform viewer = null)
        {
            DisposeCells();
            _viewer = viewer != null ? viewer : Camera.main != null ? Camera.main.transform : transform;
            Camera viewerCamera = _viewer.GetComponent<Camera>();
            if (viewerCamera != null) viewerCamera.farClipPlane = Mathf.Max(viewerCamera.farClipPlane, _terrainRenderDistance * 1.05f);
            _profile = WorldRepresentationProfile.CreateDefault();
            _hierarchy = new WorldSpatialHierarchy();
            _generator = new HierarchicalWorldGenerator(sharedHeightSource, biome, config, worldSeed, generationVersion);
            _builder = new TerrainRepresentationBuilder();
            _budget = new WorldStreamingBudget(_cpuBudgetMilliseconds, _meshCommitsPerFrame, 4, 2);
            _material = terrainMaterial;
            _origin = new GlobalPosition(0d, 0d, 0d);
            _detailedWorldBounds = new WorldBounds(-config.Width * 0.5d, -config.Depth * 0.5d, Math.Max(config.Width, config.Depth));
            _initialized = true;
            RefreshSelection(force: true);
        }

        private void Update()
        {
            if (!_initialized || _viewer == null) return;
            _viewerGlobal = _origin.Add(_viewer.position.x, _viewer.position.y, _viewer.position.z);
            TryRebase();
            if (Time.unscaledTime >= _nextSelectionTime) RefreshSelection(force: false);
            CommitCompleted();
            UpdateAtmosphere();
            UpdateSubpixelVisibility();
            _frame++;
        }

        private void UpdateSubpixelVisibility()
        {
            if (!_stabilizeDistantWorld || _viewer == null) return;
            Camera camera = _viewer.GetComponent<Camera>(); if (camera == null) return;
            foreach (CellRuntime runtime in _active.Values)
                runtime.UpdateSubpixelVisibility(camera, _subpixelThreshold);
        }

        private void RefreshSelection(bool force)
        {
            _nextSelectionTime = Time.unscaledTime + (force ? 0.02f : 0.2f);
            Stopwatch watch = Stopwatch.StartNew();
            IReadOnlyList<WorldCell> selected = _hierarchy.Select(_viewerGlobal, _profile);
            HashSet<WorldCellCoordinate> retained = new HashSet<WorldCellCoordinate>();
            int scheduled = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                WorldCell cell = selected[i];
                if (cell.Bounds.DistanceTo(_viewerGlobal.X, _viewerGlobal.Z) > _terrainRenderDistance) continue;
                retained.Add(cell.Coordinate);
                CellRuntime runtime;
                if (_active.TryGetValue(cell.Coordinate, out runtime) && runtime.Level == cell.Representation) { runtime.LastTouched = _frame; continue; }
                if (_pending.Contains(cell.Coordinate) || scheduled >= _budget.MaxGenerationJobs) continue;
                _pending.Add(cell.Coordinate); scheduled++;
                WorldRepresentationLevel level = _profile[cell.Representation];
                WorldBounds? exclusion = _detailedWorldBounds;
                Task.Run(() => _builder.Build(cell, level, _generator, exclusion)).ContinueWith(task =>
                {
                    lock (_completionLock) _completed.Enqueue(new CompletedBuild(cell, task.IsCompletedSuccessfully ? task.Result : null));
                }, TaskScheduler.Default);
            }
            List<WorldCellCoordinate> remove = new List<WorldCellCoordinate>();
            foreach (KeyValuePair<WorldCellCoordinate, CellRuntime> pair in _active)
                if (!retained.Contains(pair.Key) && _frame - pair.Value.LastTouched > 2) remove.Add(pair.Key);
            for (int i = 0; i < remove.Count; i++) Release(remove[i]);
            watch.Stop();
            _metrics = _metrics.WithSelection(selected.Count, _pending.Count, (float)watch.Elapsed.TotalMilliseconds);
        }


        private void CommitCompleted()
        {
            Stopwatch watch = Stopwatch.StartNew();
            int commits = 0;
            while (commits < _budget.MaxMeshCommitsPerFrame && watch.Elapsed.TotalMilliseconds < _budget.MaxCpuMillisecondsPerFrame)
            {
                CompletedBuild completed;
                lock (_completionLock) { if (_completed.Count == 0) break; completed = _completed.Dequeue(); }
                _pending.Remove(completed.Cell.Coordinate);
                if (completed.Data == null) continue;
                CellRuntime old;
                if (_active.TryGetValue(completed.Cell.Coordinate, out old)) Release(completed.Cell.Coordinate);
                CellRuntime runtime = Acquire();
                runtime.Apply(completed.Data, _origin, _material, _debugLodColors, _maximumVisibility ? float.MaxValue : _atmosphericVisibilityDistance);
                runtime.LastTouched = _frame;
                _active[completed.Cell.Coordinate] = runtime;
                commits++;
            }
            watch.Stop();
            int triangles = 0; int proxies = 0;
            int[] counts = new int[8];
            foreach (CellRuntime runtime in _active.Values) { triangles += runtime.TriangleCount; if (runtime.ForestProxyVisible) proxies++; counts[(int)runtime.Level]++; }
            _metrics = new DistantWorldMetrics(_active.Count, counts, triangles, proxies, _pending.Count,
                _metrics.SelectionMilliseconds, (float)watch.Elapsed.TotalMilliseconds, _origin);
        }

        private CellRuntime Acquire()
        {
            if (_pool.Count > 0) { CellRuntime item = _pool.Dequeue(); item.SetActive(true); return item; }
            return new CellRuntime(transform);
        }

        private void Release(WorldCellCoordinate coordinate)
        {
            CellRuntime runtime;
            if (!_active.TryGetValue(coordinate, out runtime)) return;
            _active.Remove(coordinate); runtime.SetActive(false); _pool.Enqueue(runtime);
        }

        private void TryRebase()
        {
            Vector3 local = _viewer.position;
            if (new Vector2(local.x, local.z).magnitude < _rebaseThreshold) return;
            GlobalPosition previous = _origin;
            Vector3 shift = new Vector3(local.x, 0f, local.z);
            _origin = _origin.Add(shift.x, 0d, shift.z);
            // The renderer lives on the common runtime root, so one rebase moves detailed terrain,
            // decorations, liquids, proxies and distant cells atomically.
            transform.position -= shift;
            _viewer.position -= shift;
            WorldRebased?.Invoke(previous, _origin);
        }

        private void UpdateAtmosphere()
        {
            float visibility = _maximumVisibility ? 1000000f : _atmosphericVisibilityDistance;
            Shader.SetGlobalFloat("_WorldAtmosphericVisibility", visibility);
            Shader.SetGlobalFloat("_WorldAtmosphereDisabled", _maximumVisibility ? 1f : 0f);
        }

        private void RefreshExistingMaterials()
        {
            foreach (CellRuntime runtime in _active.Values) runtime.SetDebugColor(_debugLodColors);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawQuadtree || _active == null) return;
            foreach (CellRuntime runtime in _active.Values) runtime.DrawBounds();
        }

        private void OnDestroy() => DisposeCells();
        private void DisposeCells()
        {
            foreach (CellRuntime runtime in _active.Values) runtime.Dispose();
            while (_pool.Count > 0) _pool.Dequeue().Dispose();
            _active.Clear(); _pending.Clear(); lock (_completionLock) _completed.Clear();
        }

        private readonly struct CompletedBuild
        {
            public CompletedBuild(WorldCell cell, TerrainRepresentationData data) { Cell = cell; Data = data; }
            public WorldCell Cell { get; }
            public TerrainRepresentationData Data { get; }
        }

        private sealed class CellRuntime : IDisposable
        {
            private readonly GameObject _root;
            private readonly Mesh _mesh;
            private readonly MeshRenderer _renderer;
            private readonly GameObject _forestProxy;
            private WorldBounds _bounds;
            private Color[] _worldColors;
            public CellRuntime(Transform parent)
            {
                _root = new GameObject("Distant World Cell"); _root.transform.SetParent(parent, false);
                MeshFilter filter = _root.AddComponent<MeshFilter>(); _renderer = _root.AddComponent<MeshRenderer>();
                _mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; filter.sharedMesh = _mesh;
                _forestProxy = GameObject.CreatePrimitive(PrimitiveType.Cube); _forestProxy.name = "Regional Forest Canopy Proxy";
                UnityEngine.Object.Destroy(_forestProxy.GetComponent<Collider>()); _forestProxy.transform.SetParent(_root.transform, false);
            }
            public WorldRepresentationKind Level { get; private set; }
            public int TriangleCount { get; private set; }
            public bool ForestProxyVisible => _forestProxy.activeSelf;
            public long LastTouched { get; set; }
            public void Apply(TerrainRepresentationData data, GlobalPosition origin, Material material, bool debug, float visibility)
            {
                Level = data.Level; _bounds = data.Bounds; _root.name = $"{data.Level} {data.Coordinate}";
                _root.transform.position = new Vector3((float)(data.Bounds.MinimumX - origin.X), 0f, (float)(data.Bounds.MinimumZ - origin.Z));
                Vector3[] vertices = new Vector3[data.Vertices.Length]; Color[] colors = new Color[vertices.Length]; _worldColors = new Color[vertices.Length];
                Color debugColor = ResolveDebugColor(data.Level);
                for (int i = 0; i < vertices.Length; i++) { WorldVector3 v = data.Vertices[i]; vertices[i] = new Vector3(v.X, v.Y, v.Z); WorldColor c = data.Colors[i]; _worldColors[i] = new Color(c.Red, c.Green, c.Blue); colors[i] = debug ? debugColor : _worldColors[i]; }
                _mesh.Clear(); _mesh.vertices = vertices; _mesh.colors = colors; _mesh.triangles = data.Triangles; _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
                _renderer.sharedMaterial = material; _renderer.shadowCastingMode = (int)data.Level >= (int)WorldRepresentationKind.Medium ? ShadowCastingMode.On : ShadowCastingMode.Off;
                _renderer.receiveShadows = (int)data.Level >= (int)WorldRepresentationKind.Medium;
                TriangleCount = data.Triangles.Length / 3;
                bool proxy = data.ForestCoverage > 0.58f && (data.Level == WorldRepresentationKind.Far || data.Level == WorldRepresentationKind.Distant);
                _forestProxy.SetActive(proxy); if (proxy) { float size = (float)data.Bounds.Size; _forestProxy.transform.localPosition = new Vector3(size * 0.5f, 45f, size * 0.5f); _forestProxy.transform.localScale = new Vector3(size * 0.82f, 38f, size * 0.82f); _forestProxy.GetComponent<MeshRenderer>().sharedMaterial = material; }
            }
            public void Shift(Vector3 shift) => _root.transform.position += shift;
            public void SetDebugColor(bool enabled)
            {
                if (_worldColors == null) return;
                Color[] colors = new Color[_worldColors.Length];
                if (enabled) { Color color = ResolveDebugColor(Level); for (int i = 0; i < colors.Length; i++) colors[i] = color; }
                else Array.Copy(_worldColors, colors, colors.Length);
                _mesh.colors = colors;
            }
            public void UpdateSubpixelVisibility(Camera camera, float thresholdPixels)
            {
                if (!_forestProxy.activeSelf) return;
                float distance = Mathf.Max(1f, Vector3.Distance(camera.transform.position, _forestProxy.transform.position));
                float projectedPixels = _forestProxy.transform.lossyScale.y / distance *
                    (camera.pixelHeight / (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)));
                _forestProxy.GetComponent<MeshRenderer>().enabled = projectedPixels >= thresholdPixels;
            }
            public void SetActive(bool active) => _root.SetActive(active);
            public void DrawBounds() { Gizmos.color = ResolveDebugColor(Level); Vector3 c = _root.transform.position + new Vector3((float)_bounds.Size * 0.5f, 0f, (float)_bounds.Size * 0.5f); Gizmos.DrawWireCube(c, new Vector3((float)_bounds.Size, 20f, (float)_bounds.Size)); }
            public void Dispose() { if (_root != null) UnityEngine.Object.Destroy(_root); if (_mesh != null) UnityEngine.Object.Destroy(_mesh); }
            private static Color ResolveDebugColor(WorldRepresentationKind level)
            { switch (level) { case WorldRepresentationKind.Simulation: return Color.red; case WorldRepresentationKind.Near: return new Color(1f, .5f, 0f); case WorldRepresentationKind.Medium: return Color.yellow; case WorldRepresentationKind.Far: return Color.green; case WorldRepresentationKind.Distant: return Color.cyan; default: return new Color(.35f, .4f, 1f); } }
        }
    }

    public readonly struct DistantWorldMetrics
    {
        public DistantWorldMetrics(int activeCells, int[] counts, int terrainTriangles, int forestProxies, int pendingJobs,
            float selectionMilliseconds, float commitMilliseconds, GlobalPosition origin)
        { ActiveCells = activeCells; Counts = counts; TerrainTriangles = terrainTriangles; ForestProxies = forestProxies; PendingJobs = pendingJobs; SelectionMilliseconds = selectionMilliseconds; CommitMilliseconds = commitMilliseconds; Origin = origin; }
        public int ActiveCells { get; }
        public int[] Counts { get; }
        public int TerrainTriangles { get; }
        public int ForestProxies { get; }
        public int PendingJobs { get; }
        public float SelectionMilliseconds { get; }
        public float CommitMilliseconds { get; }
        public GlobalPosition Origin { get; }
        public int Count(WorldRepresentationKind level) => Counts != null && (int)level < Counts.Length ? Counts[(int)level] : 0;
        public DistantWorldMetrics WithSelection(int activeCells, int pendingJobs, float milliseconds) => new DistantWorldMetrics(activeCells, Counts, TerrainTriangles, ForestProxies, pendingJobs, milliseconds, CommitMilliseconds, Origin);
    }
}
