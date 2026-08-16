using System;
using System.Collections.Generic;
using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class EnvironmentalVfxSystem : IDisposable
    {
        private readonly EnvironmentalVfxPool _pool;
        private readonly List<Cell> _cells = new List<Cell>();
        private EnvironmentalSurfaceResolver _surfaceResolver;
        private readonly BiomeEnvironmentalResponseProfile[] _profiles;
        private float _nextRefresh;
        private float _nextRareEvent;
        public EnvironmentalVfxSystem(Transform parent, Material material, int capacity, BiomeEnvironmentalResponseProfile[] profiles = null)
        { _pool = new EnvironmentalVfxPool(parent, material, capacity); _profiles = profiles ?? Array.Empty<BiomeEnvironmentalResponseProfile>(); }
        public int ActiveCellCount => _pool.ActiveCount;
        public float DensityMultiplier { get; set; } = 1f;
        public EnvironmentalBiomeKind Biome { get; set; } = EnvironmentalBiomeKind.Grassland;

        public void Configure(ZoneGenerationResult zone)
        {
            _surfaceResolver = new EnvironmentalSurfaceResolver(zone); _cells.Clear(); _pool.ReleaseAll();
            TerrainGenerationConfig config = zone.Terrain.Config;
            float width = config.Width / config.ChunkCountX; float depth = config.Depth / config.ChunkCountZ;
            for (int z = 0; z < config.ChunkCountZ; z++) for (int x = 0; x < config.ChunkCountX; x++)
            {
                Vector3 center = new Vector3(-config.Width * 0.5f + (x + 0.5f) * width, 0f, -config.Depth * 0.5f + (z + 0.5f) * depth);
                center.y = zone.Terrain.HeightField.SampleHeight(center.x, center.z) + 1.2f;
                _cells.Add(new Cell(center, new Vector3(width, 3f, depth)));
            }
        }

        public void Tick(float time, Camera camera, WindSystem wind)
        {
            if (camera == null || wind == null || _surfaceResolver == null || time < _nextRefresh) return;
            _nextRefresh = time + 0.35f; _pool.ReleaseAll();
            _cells.Sort((a, b) => Vector3.SqrMagnitude(a.Center - camera.transform.position).CompareTo(Vector3.SqrMagnitude(b.Center - camera.transform.position)));
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            for (int index = 0; index < _cells.Count && _pool.ActiveCount < _pool.Capacity; index++)
            {
                Cell cell = _cells[index]; float distance = Vector3.Distance(camera.transform.position, cell.Center);
                if (distance > 150f || !GeometryUtility.TestPlanesAABB(planes, new Bounds(cell.Center, cell.Size))) continue;
                EnvironmentalSurfaceKind surface = _surfaceResolver.Resolve(cell.Center, Biome);
                if (!TryResolveConfiguredRule(Biome, surface, out EnvironmentalVfxRule rule)) continue;
                WindSample sample = wind.SampleWind(cell.Center); float response = rule.Evaluate(sample.EffectiveStrength);
                if (response <= 0.001f) continue;
                float lod = distance < 30f ? 1f : distance < 80f ? 0.45f : 0.16f;
                EnvironmentalVfxEmitter emitter = _pool.Acquire();
                emitter.Configure(cell.Center, cell.Size, sample, rule, response * lod * DensityMultiplier);
                if (time >= _nextRareEvent && sample.EffectiveStrength >= 0.65f && rule.RareEventProbability > 0f &&
                    Mathf.PerlinNoise(cell.Center.x * 0.013f + time * 0.07f, cell.Center.z * 0.017f) > 1f - rule.RareEventProbability)
                { emitter.EmitBurst(Mathf.CeilToInt(4f + 12f * response)); _nextRareEvent = time + rule.Cooldown; }
            }
        }

        private bool TryResolveConfiguredRule(EnvironmentalBiomeKind biome, EnvironmentalSurfaceKind surface, out EnvironmentalVfxRule rule)
        {
            for (int index = 0; index < _profiles.Length; index++)
                if (_profiles[index] != null && _profiles[index].Biome == biome && _profiles[index].TryResolve(surface, out rule)) return true;
            return TryResolveRule(biome, surface, out rule);
        }

        public static bool TryResolveRule(EnvironmentalBiomeKind biome, EnvironmentalSurfaceKind surface, out EnvironmentalVfxRule rule)
        {
            if (surface == EnvironmentalSurfaceKind.Water || surface == EnvironmentalSurfaceKind.Rock || surface == EnvironmentalSurfaceKind.Concrete)
            { rule = default; return false; }
            switch (biome)
            {
                case EnvironmentalBiomeKind.Desert when surface == EnvironmentalSurfaceKind.Sand:
                    rule = new EnvironmentalVfxRule(EnvironmentalVfxKind.SandDust, 0.12f, 24f, 1.25f, 0.34f, 2.4f, 0.18f); return true;
                case EnvironmentalBiomeKind.Forest when surface == EnvironmentalSurfaceKind.Grass:
                    rule = new EnvironmentalVfxRule(EnvironmentalVfxKind.DryLeaves, 0.28f, 9f, 0.9f, 0.2f, 3.2f, 0.12f); return true;
                case EnvironmentalBiomeKind.Snow when surface == EnvironmentalSurfaceKind.Snow:
                    rule = new EnvironmentalVfxRule(EnvironmentalVfxKind.LooseSnow, 0.1f, 20f, 1.1f, 0.16f, 3.4f); return true;
                case EnvironmentalBiomeKind.Grassland when surface == EnvironmentalSurfaceKind.Grass:
                    rule = new EnvironmentalVfxRule(EnvironmentalVfxKind.Pollen, 0.08f, 7f, 0.55f, 0.09f, 3.8f); return true;
                default: rule = default; return false;
            }
        }

        public void Dispose() => _pool.Dispose();
        private readonly struct Cell { public Cell(Vector3 center, Vector3 size) { Center = center; Size = size; } public Vector3 Center { get; } public Vector3 Size { get; } }
    }

    internal sealed class EnvironmentalVfxPool : IDisposable
    {
        private readonly List<EnvironmentalVfxEmitter> _all = new List<EnvironmentalVfxEmitter>();
        private readonly Stack<EnvironmentalVfxEmitter> _available = new Stack<EnvironmentalVfxEmitter>();
        private readonly Mesh _particleMesh;
        public EnvironmentalVfxPool(Transform parent, Material material, int capacity)
        {
            Capacity = Mathf.Max(1, capacity);
            _particleMesh = CreateParticleMesh();
            for (int index = 0; index < Capacity; index++) { var emitter = new EnvironmentalVfxEmitter(parent, material, _particleMesh, index); _all.Add(emitter); _available.Push(emitter); }
        }
        public int Capacity { get; } public int ActiveCount => Capacity - _available.Count;
        public EnvironmentalVfxEmitter Acquire() { var item = _available.Pop(); item.SetActive(true); return item; }
        public void ReleaseAll() { for (int i = 0; i < _all.Count; i++) if (_all[i].Active) { _all[i].SetActive(false); _available.Push(_all[i]); } }
        public void Dispose() { for (int i = 0; i < _all.Count; i++) _all[i].Dispose(); _all.Clear(); _available.Clear(); if (_particleMesh != null) UnityEngine.Object.Destroy(_particleMesh); }
        private static Mesh CreateParticleMesh()
        {
            Mesh mesh = new Mesh { name = "Stylized Environmental Particle" };
            mesh.vertices = new[] { new Vector3(0f,.65f,0f), new Vector3(-.42f,0f,-.22f), new Vector3(.42f,0f,-.22f), new Vector3(0f,0f,.46f), new Vector3(0f,-.3f,0f) };
            mesh.triangles = new[] { 0,2,1, 0,3,2, 0,1,3, 4,1,2, 4,2,3, 4,3,1 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }

    internal sealed class EnvironmentalVfxEmitter : IDisposable
    {
        private readonly GameObject _root; private readonly ParticleSystem _particles;
        public EnvironmentalVfxEmitter(Transform parent, Material material, Mesh mesh, int index)
        {
            _root = new GameObject($"Environmental VFX Emitter {index}"); _root.transform.SetParent(parent, false);
            _particles = _root.AddComponent<ParticleSystem>(); var main = _particles.main; main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 180;
            var renderer = _root.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = mesh;
            SetActive(false);
        }
        public bool Active => _root.activeSelf;
        public void SetActive(bool active) { if (!active) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); _root.SetActive(active); }
        public void Configure(Vector3 center, Vector3 size, WindSample wind, EnvironmentalVfxRule rule, float intensity)
        {
            _root.transform.position = center; var main = _particles.main;
            main.startLifetime = rule.Lifetime; main.startSpeed = 0f; main.startSize = rule.Size; main.startColor = ResolveColor(rule.Kind);
            var emission = _particles.emission; emission.rateOverTime = rule.Density * intensity * (1f + wind.Gust * 1.5f);
            var shape = _particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = size;
            var velocity = _particles.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            float speed = rule.Speed * wind.Speed * Mathf.Lerp(0.35f, 1f, intensity);
            velocity.x = wind.Direction.x * speed; velocity.y = 0.12f + wind.Turbulence * 0.55f; velocity.z = wind.Direction.z * speed;
            var noise = _particles.noise; noise.enabled = true; noise.strength = wind.Turbulence * 0.7f; noise.frequency = 0.35f;
            if (!_particles.isPlaying) _particles.Play();
        }
        public void EmitBurst(int count) { if (count > 0) _particles.Emit(count); }
        private static Color ResolveColor(EnvironmentalVfxKind kind)
        {
            switch (kind) { case EnvironmentalVfxKind.SandDust: return new Color(0.82f, 0.64f, 0.3f, 0.62f); case EnvironmentalVfxKind.DryLeaves: return new Color(0.72f, 0.34f, 0.08f, 0.9f); case EnvironmentalVfxKind.LooseSnow: return new Color(0.9f, 0.96f, 1f, 0.72f); default: return new Color(0.9f, 0.78f, 0.3f, 0.65f); }
        }
        public void Dispose() { if (_root != null) UnityEngine.Object.Destroy(_root); }
    }
}
