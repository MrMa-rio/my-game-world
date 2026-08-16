using System;
using System.Collections.Generic;
using System.Diagnostics;
using MyGameWorld.Shared.World;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using UnityEngine;
using UnityEngine.Rendering;
using MyGameWorld.Client.ActorRuntime;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class ProceduralRuntimeManager : MonoBehaviour
    {
        [SerializeField] private ProceduralStyleProfile _style = new ProceduralStyleProfile();
        [SerializeField] private ProceduralGenerationBudget _budget = new ProceduralGenerationBudget();
        [SerializeField] private bool _enableDynamicLod = true;
        [SerializeField, Min(0.1f)] private float _lodRefreshInterval = 0.5f;

        private readonly Queue<GenerationWorkItem> _high = new Queue<GenerationWorkItem>();
        private readonly Queue<GenerationWorkItem> _normal = new Queue<GenerationWorkItem>();
        private readonly Queue<GenerationWorkItem> _low = new Queue<GenerationWorkItem>();
        private readonly List<RuntimeInstance> _active = new List<RuntimeInstance>();
        private readonly Dictionary<DecorationKind, Stack<GameObject>> _pools = new Dictionary<DecorationKind, Stack<GameObject>>();
        private readonly List<IProceduralGeometryProvider> _providers = new List<IProceduralGeometryProvider>();
        private readonly ProceduralMeshCache _cache = new ProceduralMeshCache();
        private readonly ProceduralLodResolver _lodResolver = new ProceduralLodResolver();
        private Transform _instanceParent;
        private ProceduralWorldMaterialLibrary _materials;
        private IAssetRegistry<UnityEngine.Object> _assetRegistry;
        private float _nextLodRefresh;
        private int _lodCursor;
        private int _generatedMeshes;
        private int _resolvedFiniteAssets;
        private int _cacheHits;
        private int _cacheMisses;
        private float _lastFrameMilliseconds;
        private bool _initialized;
        private EnvironmentalPhysicalResponseSystem _environmentalResponses;

        public ProceduralRuntimeMetrics Metrics
        {
            get
            {
                int vertices = 0; int triangles = 0; int drawCalls = 0;
                for (int i = 0; i < _active.Count; i++)
                {
                    if (!_active[i].Active || _active[i].Resource == null) continue;
                    vertices += _active[i].Resource.VertexCount; triangles += _active[i].Resource.TriangleCount;
                    drawCalls += _active[i].Resource.Mesh.subMeshCount;
                }
                return new ProceduralRuntimeMetrics(ActiveCount, QueueCount, _cache.Count, _generatedMeshes, _resolvedFiniteAssets,
                    _cacheHits, _cacheMisses, vertices, triangles, drawCalls, _lastFrameMilliseconds);
            }
        }

        public int ActiveCount
        {
            get { int count = 0; for (int i = 0; i < _active.Count; i++) if (_active[i].Active) count++; return count; }
        }
        public int QueueCount => _high.Count + _normal.Count + _low.Count;

        public void Initialize(ProceduralWorldMaterialLibrary materials, IAssetRegistry<UnityEngine.Object> assetRegistry = null)
        {
            if (_initialized) return;
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _assetRegistry = assetRegistry;
            RegisterGeometryProvider(new NaturalDecorationGeometryProvider());
            _initialized = true;
        }

        public void SetInstanceParent(Transform parent) => _instanceParent = parent;
        public void SetEnvironmentalResponses(EnvironmentalPhysicalResponseSystem responses) => _environmentalResponses = responses;
        public void ConfigureImageStability(float lodBias, float subpixelThreshold) => _lodResolver.ConfigureImageStability(lodBias, subpixelThreshold);

        public void RegisterGeometryProvider(IProceduralGeometryProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _providers.Add(provider);
        }

        public UnityTerrainChunkRuntime MaterializeTerrainChunk(TerrainChunkData data, TerrainSurfaceDNA identity, Material wireframeMaterial)
        {
            EnsureInitialized();
            return new UnityTerrainChunkRuntime(data, _instanceParent, _materials.Terrain, wireframeMaterial, identity, _style.StyleVersion);
        }

        public void Request(ProceduralGenerationRequest request)
        {
            EnsureInitialized();
            Enqueue(new GenerationWorkItem(request), request.Priority);
        }

        public ProceduralVisualLod ResolveLod(DecorationPlacement definition, Vector3 viewerPosition) => _lodResolver.Resolve(definition, viewerPosition);

        public int Count(DecorationKind kind)
        {
            int count = 0;
            for (int i = 0; i < _active.Count; i++) if (_active[i].Active && _active[i].Definition.Kind == kind) count++;
            return count;
        }

        public void ReleaseAll()
        {
            _high.Clear(); _normal.Clear(); _low.Clear();
            for (int i = 0; i < _active.Count; i++) Release(_active[i]);
            _active.Clear(); _lodCursor = 0;
        }

        public void FlushQueue()
        {
            while (QueueCount > 0) ProcessOne(Dequeue());
        }

        private void Update()
        {
            if (!_initialized) return;
            ProcessFrameBudget();
            if (_enableDynamicLod && Time.unscaledTime >= _nextLodRefresh)
            {
                _nextLodRefresh = Time.unscaledTime + _lodRefreshInterval;
                QueueLodUpdates();
            }
        }

        private void OnDestroy()
        {
            _high.Clear(); _normal.Clear(); _low.Clear();
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Root != null) DestroyRuntimeObject(_active[i].Root);
            _active.Clear();
            foreach (Stack<GameObject> pool in _pools.Values)
                while (pool.Count > 0) { GameObject item = pool.Pop(); if (item != null) DestroyRuntimeObject(item); }
            _cache.Dispose();
        }

        private void ProcessFrameBudget()
        {
            Stopwatch stopwatch = Stopwatch.StartNew(); int objects = 0; int vertices = 0;
            while (QueueCount > 0 && objects < _budget.MaxObjectsPerFrame)
            {
                GenerationWorkItem work = Peek();
                int estimate = _lodResolver.EstimateVertexCount(work.Request.Definition.Kind, work.Request.DesiredLod);
                if (objects > 0 && vertices + estimate > _budget.MaxVerticesPerFrame) break;
                ProcessOne(Dequeue()); objects++; vertices += estimate;
                if (stopwatch.Elapsed.TotalMilliseconds >= _budget.MaxMillisecondsPerFrame) break;
            }
            stopwatch.Stop(); _lastFrameMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
        }

        private void ProcessOne(GenerationWorkItem work)
        {
            if (work.Target != null && (!work.Target.Active || !work.Target.LodQueued)) return;
            Stopwatch stopwatch = Stopwatch.StartNew();
            ProceduralMeshKey key = CreateKey(work.Request.Definition, work.Request.DesiredLod);
            ProceduralMeshResource resource;
            bool cacheHit = _cache.TryGet(key, out resource);
            if (cacheHit) _cacheHits++;
            else
            {
                _cacheMisses++;
                resource = TryResolveFiniteAsset(work.Request.Definition, key);
                if (resource != null) _resolvedFiniteAssets++;
                else { resource = ResolveProvider(work.Request.Definition.Kind).Build(key, _style, _lodResolver); _generatedMeshes++; }
                _cache.Add(resource);
            }
            stopwatch.Stop();

            if (work.Target != null)
            {
                ApplyResource(work.Target, resource, work.Request.DesiredLod);
                work.Target.Root.GetComponent<ProceduralRuntimeDebugInfo>().UpdateInfo(resource, work.Request.DesiredLod, cacheHit, (float)stopwatch.Elapsed.TotalMilliseconds);
                work.Target.LodQueued = false;
                return;
            }

            RuntimeInstance instance = CreateInstance(work.Request, resource);
            instance.Root.GetComponent<ProceduralRuntimeDebugInfo>().UpdateInfo(resource, work.Request.DesiredLod, cacheHit, (float)stopwatch.Elapsed.TotalMilliseconds);
            _active.Add(instance);
        }

        private RuntimeInstance CreateInstance(ProceduralGenerationRequest request, ProceduralMeshResource resource)
        {
            DecorationPlacement definition = request.Definition;
            GameObject root = GetPooled(definition.Kind);
            root.name = $"{definition.Kind} {definition.ElementId.Value}";
            root.transform.SetParent(_instanceParent, false);
            root.transform.localPosition = new Vector3(definition.Position.X, definition.Position.Y, definition.Position.Z);
            Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, request.Environment.SurfaceNormal);
            float slopeInfluence = ResolveSlopeInfluence(definition.Kind);
            root.transform.localRotation = Quaternion.Slerp(Quaternion.identity, slopeRotation, slopeInfluence) * Quaternion.Euler(0f, definition.YawDegrees, 0f);
            root.transform.localScale = new Vector3(definition.Scale * definition.ShapeA, definition.Scale * definition.ShapeB, definition.Scale * definition.ShapeC);
            root.GetComponent<WorldElementRuntimeIdentity>().Initialize(definition);
            RuntimeInstance instance = new RuntimeInstance(root, definition, request.Environment);
            ApplyResource(instance, resource, request.DesiredLod);
            ConfigureCollider(instance);
            ApplyColorVariation(instance);
            root.SetActive(true); instance.Active = true;
            _environmentalResponses?.Register(root, definition.Kind);
            return instance;
        }

        private GameObject GetPooled(DecorationKind kind)
        {
            Stack<GameObject> pool;
            if (_pools.TryGetValue(kind, out pool) && pool.Count > 0) return pool.Pop();
            GameObject root = new GameObject();
            root.AddComponent<MeshFilter>();
            MeshRenderer renderer = root.AddComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            root.AddComponent<WorldElementRuntimeIdentity>();
            root.AddComponent<ProceduralRuntimeDebugInfo>();
            root.AddComponent<CapsuleCollider>().enabled = false;
            root.AddComponent<BoxCollider>().enabled = false;
            return root;
        }

        private void ApplyResource(RuntimeInstance instance, ProceduralMeshResource resource, ProceduralVisualLod lod)
        {
            instance.Root.GetComponent<MeshFilter>().sharedMesh = resource.Mesh;
            instance.Root.GetComponent<MeshRenderer>().sharedMaterials = resource.Materials ?? ResolveMaterials(instance.Definition.Kind, resource.Mesh.subMeshCount);
            instance.Resource = resource; instance.Lod = lod;
        }

        private Material[] ResolveMaterials(DecorationKind kind, int count)
        {
            switch (kind)
            {
                case DecorationKind.Tree: return _materials.TreeMaterials;
                case DecorationKind.TreeCluster: return _materials.TreeMaterials;
                case DecorationKind.Bush: return count == 2 ? _materials.BushMaterials : _materials.BushLowMaterials;
                case DecorationKind.Rock: return _materials.RockMaterials;
                case DecorationKind.RockCluster: return _materials.RockMaterials;
                case DecorationKind.BushCluster: return count == 2 ? _materials.BushMaterials : _materials.BushLowMaterials;
                case DecorationKind.Flower:
                case DecorationKind.FlowerCluster: return _materials.FlowerMaterials;
                case DecorationKind.Mushroom:
                case DecorationKind.MushroomCluster: return _materials.MushroomMaterials;
                default: return _materials.MarkerMaterials;
            }
        }

        private static float ResolveSlopeInfluence(DecorationKind kind)
        {
            switch (kind)
            {
                case DecorationKind.Rock: return 0.9f;
                case DecorationKind.RockCluster: return 0.75f;
                case DecorationKind.Bush: return 0.3f;
                case DecorationKind.BushCluster: return 0.25f;
                case DecorationKind.Mushroom:
                case DecorationKind.MushroomCluster: return 0.2f;
                case DecorationKind.Flower:
                case DecorationKind.FlowerCluster: return 0.1f;
                default: return 0f;
            }
        }

        private static void ConfigureCollider(RuntimeInstance instance)
        {
            CapsuleCollider capsule = instance.Root.GetComponent<CapsuleCollider>();
            BoxCollider box = instance.Root.GetComponent<BoxCollider>();
            MeshFilter filter = instance.Root.GetComponent<MeshFilter>();
            Bounds bounds = filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
            capsule.enabled = false; box.enabled = false;
            instance.Root.layer = WorldPhysicsLayers.SoftEnvironment;
            if (instance.Definition.Kind == DecorationKind.Tree)
            {
                instance.Root.layer = WorldPhysicsLayers.StaticWorld;
                capsule.enabled = true;
                capsule.center = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.34f, bounds.center.z);
                capsule.height = Mathf.Max(0.5f, bounds.size.y * 0.68f);
                capsule.radius = Mathf.Max(0.12f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.16f);
            }
            else if (instance.Definition.Kind != DecorationKind.ScaleMarker)
            {
                box.enabled = true;
                switch (instance.Definition.Kind)
                {
                    case DecorationKind.Rock:
                    case DecorationKind.TreeCluster:
                    case DecorationKind.RockCluster:
                        instance.Root.layer = WorldPhysicsLayers.StaticWorld;
                        break;
                }

                box.center = bounds.center;
                box.size = Vector3.Max(bounds.size * (instance.Root.layer == WorldPhysicsLayers.StaticWorld ? 0.88f : 0.72f), Vector3.one * 0.08f);
                box.isTrigger = instance.Root.layer == WorldPhysicsLayers.SoftEnvironment;
            }
        }

        private void ApplyColorVariation(RuntimeInstance instance)
        {
            uint bits = unchecked((uint)(instance.Definition.Seed ^ (instance.Definition.Seed >> 32)));
            float signed = ((bits & 1023u) / 1023f * 2f - 1f) * _style.ColorVariation;
            float stress = instance.Environment.Slope * 0.08f;
            Color tint = new Color(1f + signed - stress, 1f + signed * 0.45f - stress, 1f - signed * 0.25f, 1f);
            MaterialPropertyBlock block = new MaterialPropertyBlock(); block.SetColor("_InstanceColor", tint);
            instance.Root.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }

        private void QueueLodUpdates()
        {
            Camera camera = Camera.main; if (camera == null || _active.Count == 0) return;
            int checkedCount = 0;
            while (checkedCount < _budget.LodChecksPerFrame && _active.Count > 0)
            {
                if (_lodCursor >= _active.Count) _lodCursor = 0;
                RuntimeInstance instance = _active[_lodCursor++]; checkedCount++;
                if (!instance.Active || instance.LodQueued) continue;
                ProceduralVisualLod desired = _lodResolver.ResolveStable(instance.Definition, camera.transform.position, instance.Lod);
                if (desired == instance.Lod) continue;
                instance.LodQueued = true;
                ProceduralGenerationRequest request = new ProceduralGenerationRequest(instance.Definition, instance.Environment, desired, GenerationPriority.Low);
                Enqueue(new GenerationWorkItem(request, instance), GenerationPriority.Low);
            }
        }

        private void Release(RuntimeInstance instance)
        {
            if (!instance.Active) return;
            _environmentalResponses?.Unregister(instance.Root);
            instance.Active = false; instance.LodQueued = false; instance.Root.SetActive(false);
            instance.Root.transform.SetParent(transform, false);
            Stack<GameObject> pool;
            if (!_pools.TryGetValue(instance.Definition.Kind, out pool)) { pool = new Stack<GameObject>(); _pools.Add(instance.Definition.Kind, pool); }
            pool.Push(instance.Root);
        }

        private ProceduralMeshKey CreateKey(DecorationPlacement definition, ProceduralVisualLod lod)
        {
            ulong seed = unchecked((ulong)definition.Seed); int variants = Mathf.Max(1, _style.GeometryVariantsPerKind);
            byte variation = (byte)((seed ^ (seed >> 32)) % (uint)variants);
            return new ProceduralMeshKey(definition.Kind, definition.VisualAssetId.Value, lod, variation, _style.StyleVersion);
        }

        private IProceduralGeometryProvider ResolveProvider(DecorationKind kind)
        {
            for (int i = 0; i < _providers.Count; i++) if (_providers[i].Supports(kind)) return _providers[i];
            throw new InvalidOperationException($"No procedural geometry provider supports {kind}.");
        }

        private ProceduralMeshResource TryResolveFiniteAsset(DecorationPlacement definition, ProceduralMeshKey key)
        {
            if (_assetRegistry == null) return null;
            if (_assetRegistry.Version != definition.AssetCatalogVersion)
                throw new InvalidOperationException($"Asset catalog version {_assetRegistry.Version} cannot materialize element catalog {definition.AssetCatalogVersion}.");
            UnityEngine.Object asset;
            if (!_assetRegistry.TryResolve(definition.VisualAssetId, out asset)) return null;
            Mesh directMesh = asset as Mesh;
            if (directMesh != null) return new ProceduralMeshResource(directMesh, key, false);
            GameObject prefab = asset as GameObject;
            if (prefab == null) return null;
            MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>();
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter == null || filter.sharedMesh == null) return null;
            return new ProceduralMeshResource(filter.sharedMesh, key, false, renderer != null ? renderer.sharedMaterials : null);
        }

        private void Enqueue(GenerationWorkItem work, GenerationPriority priority)
        {
            if (priority == GenerationPriority.High) _high.Enqueue(work); else if (priority == GenerationPriority.Normal) _normal.Enqueue(work); else _low.Enqueue(work);
        }
        private GenerationWorkItem Peek() => _high.Count > 0 ? _high.Peek() : _normal.Count > 0 ? _normal.Peek() : _low.Peek();
        private GenerationWorkItem Dequeue() => _high.Count > 0 ? _high.Dequeue() : _normal.Count > 0 ? _normal.Dequeue() : _low.Dequeue();
        private void EnsureInitialized() { if (!_initialized) throw new InvalidOperationException("ProceduralRuntimeManager.Initialize must be called first."); }
        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }

        private sealed class GenerationWorkItem
        {
            public GenerationWorkItem(ProceduralGenerationRequest request, RuntimeInstance target = null) { Request = request; Target = target; }
            public ProceduralGenerationRequest Request { get; }
            public RuntimeInstance Target { get; }
        }

        private sealed class RuntimeInstance
        {
            public RuntimeInstance(GameObject root, DecorationPlacement definition, ProceduralEnvironmentContext environment) { Root = root; Definition = definition; Environment = environment; }
            public GameObject Root { get; }
            public DecorationPlacement Definition { get; }
            public ProceduralEnvironmentContext Environment { get; }
            public ProceduralMeshResource Resource { get; set; }
            public ProceduralVisualLod Lod { get; set; }
            public bool Active { get; set; }
            public bool LodQueued { get; set; }
        }
    }
}
