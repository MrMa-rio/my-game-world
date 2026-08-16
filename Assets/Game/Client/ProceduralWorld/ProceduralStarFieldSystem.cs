using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum CelestialItemKind : byte { Star = 1 }

    public readonly struct ProceduralStar
    {
        public ProceduralStar(long itemId, long seed, Vector3 direction, float magnitude, float apparentRadius, Color color, bool clustered, float densityRank)
        { ItemId = itemId; Seed = seed; Direction = direction; Magnitude = magnitude; ApparentRadius = apparentRadius; Color = color; Clustered = clustered; DensityRank = densityRank; }
        public long ItemId { get; }
        public long Seed { get; }
        public CelestialItemKind Kind => CelestialItemKind.Star;
        public Vector3 Direction { get; }
        public float Magnitude { get; }
        public float ApparentRadius { get; }
        public Color Color { get; }
        public bool Clustered { get; }
        public float DensityRank { get; }
    }

    public sealed class ProceduralStarFieldSystem : IDisposable
    {
        private static readonly int Visibility = Shader.PropertyToID("_StarFieldVisibility");
        private const float Radius = 320f;
        private readonly GameObject _root;
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly GameObject _nebulaRoot;
        private readonly Mesh _nebulaMesh;
        private readonly Material _nebulaMaterial;
        private readonly List<ProceduralStar> _stars;
        private Light[] _sampledLights = Array.Empty<Light>();
        private float _nextLightRefresh;
        private float _localLuminosity;
        private float _densityMultiplier = 1f;

        public ProceduralStarFieldSystem(Transform parent, long worldSeed)
        {
            long fieldSeed = SeedDerivation.Derive(worldSeed, 0x53544152, 1);
            DeterministicRandom random = new DeterministicRandom(fieldSeed);
            _stars = GenerateStars(fieldSeed, random);
            Shader shader = Shader.Find("MyGameWorld/Procedural World/Star Field");
            if (shader == null) throw new InvalidOperationException("Procedural star field shader was not found.");
            _material = new Material(shader) { name = "Runtime Procedural Star Field" };
            _mesh = BuildMesh(_stars);
            _root = new GameObject("Procedural Star Field"); _root.transform.SetParent(parent, false);
            MeshFilter filter = _root.AddComponent<MeshFilter>(); filter.sharedMesh = _mesh;
            MeshRenderer renderer = _root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            Shader nebulaShader = Shader.Find("MyGameWorld/Procedural World/Nebula Field");
            if (nebulaShader == null) throw new InvalidOperationException("Procedural nebula shader was not found.");
            _nebulaMaterial = new Material(nebulaShader) { name = "Runtime Procedural Nebula Field" };
            _nebulaMesh = BuildNebulaMesh(new DeterministicRandom(SeedDerivation.Derive(fieldSeed, 0x4E454255, 1)));
            _nebulaRoot = new GameObject("Procedural Nebula Field"); _nebulaRoot.transform.SetParent(parent, false);
            MeshFilter nebulaFilter = _nebulaRoot.AddComponent<MeshFilter>(); nebulaFilter.sharedMesh = _nebulaMesh;
            MeshRenderer nebulaRenderer = _nebulaRoot.AddComponent<MeshRenderer>(); nebulaRenderer.sharedMaterial = _nebulaMaterial;
            nebulaRenderer.shadowCastingMode = ShadowCastingMode.Off; nebulaRenderer.receiveShadows = false;
        }

        public int StarCount => _stars.Count;
        public IReadOnlyList<ProceduralStar> Stars => _stars;
        public float LocalLuminosity => _localLuminosity;
        public float DensityMultiplier => _densityMultiplier;
        public float NebulaVisibility { get; private set; }
        public int EstimatedVisibleCount => Mathf.RoundToInt(_stars.Count * (_densityMultiplier / 30f));

        public void Tick(WorldTimeSnapshot time, Camera camera, Quaternion celestialRotation)
        {
            if (camera == null) { _root.SetActive(false); _nebulaRoot.SetActive(false); NebulaVisibility = 0f; return; }
            float reveal = time.StarVisibility;
            _root.SetActive(reveal > 0.001f);
            if (!_root.activeSelf) { _nebulaRoot.SetActive(false); NebulaVisibility = 0f; return; }
            _root.transform.position = camera.transform.position;
            float celestialRadiusScale = Mathf.Max(1f, camera.farClipPlane * 0.92f / Radius);
            _root.transform.localScale = Vector3.one * celestialRadiusScale;
            _root.transform.rotation = celestialRotation;
            _nebulaRoot.SetActive(true); _nebulaRoot.transform.position = _root.transform.position;
            _nebulaRoot.transform.localScale = _root.transform.localScale; _nebulaRoot.transform.rotation = celestialRotation;
            float cutoff = Mathf.Lerp(1.02f, 0.08f, Mathf.SmoothStep(0f, 1f, reveal));
            _localLuminosity = SampleLocalLuminosity(camera.transform.position);
            _densityMultiplier = ResolveDensityMultiplier(_localLuminosity);
            float darkness = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.65f, _localLuminosity));
            NebulaVisibility = reveal * darkness;
            float densityThreshold = _densityMultiplier / 30f;
            _material.SetVector(Visibility, new Vector4(reveal, cutoff, densityThreshold, 0f));
            _nebulaMaterial.SetFloat("_Visibility", NebulaVisibility);
        }

        public void Dispose()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            if (_nebulaRoot != null) UnityEngine.Object.Destroy(_nebulaRoot);
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
            if (_nebulaMesh != null) UnityEngine.Object.Destroy(_nebulaMesh);
            if (_material != null) UnityEngine.Object.Destroy(_material);
            if (_nebulaMaterial != null) UnityEngine.Object.Destroy(_nebulaMaterial);
        }

        private static List<ProceduralStar> GenerateStars(long fieldSeed, DeterministicRandom layoutRandom)
        {
            List<ProceduralStar> stars = new List<ProceduralStar>(11400);
            long itemId = 1;
            for (int layer = 0; layer < 30; layer++)
            {
                float layerStart = layer / 30f; float layerEnd = (layer + 1f) / 30f;
                for (int index = 0; index < 220; index++, itemId++)
                {
                    long itemSeed = SeedDerivation.Derive(fieldSeed, 0x53545249, itemId);
                    DeterministicRandom itemRandom = new DeterministicRandom(itemSeed);
                    float rank = Mathf.Lerp(layerStart, layerEnd, (float)itemRandom.NextUnitDouble());
                    stars.Add(CreateStar(itemId, itemSeed, RandomDirection(itemRandom), itemRandom, false, index < 12, rank));
                }
                for (int cluster = 0; cluster < 8; cluster++)
                {
                    Vector3 anchor = RandomDirection(layoutRandom); float spread = Range(layoutRandom, 0.025f, 0.095f);
                    int count = 14 + layoutRandom.NextInt(11);
                    for (int index = 0; index < count; index++, itemId++)
                    {
                        long itemSeed = SeedDerivation.Derive(fieldSeed, 0x53545249, itemId);
                        DeterministicRandom itemRandom = new DeterministicRandom(itemSeed);
                        Vector3 offset = RandomInsideSphere(itemRandom) * spread;
                        float rank = Mathf.Lerp(layerStart, layerEnd, (float)itemRandom.NextUnitDouble());
                        stars.Add(CreateStar(itemId, itemSeed, (anchor + offset).normalized, itemRandom, true, index == 0 && cluster < 4, rank));
                    }
                }
            }
            return stars;
        }

        private static ProceduralStar CreateStar(long itemId, long seed, Vector3 direction, DeterministicRandom random, bool clustered, bool highlight, float densityRank)
        {
            float magnitude = highlight ? Range(random, 0.92f, 1f) : Range(random, clustered ? 0.24f : 0.16f, 0.91f);
            float size = Mathf.Lerp(1.05f, 2.65f, magnitude) * Range(random, 0.86f, 1.16f);
            Color cool = new Color(0.58f, 0.72f, 1f); Color warm = new Color(1f, 0.76f, 0.48f);
            Color color = Color.Lerp(cool, warm, (float)random.NextUnitDouble() * 0.34f);
            return new ProceduralStar(itemId, seed, direction, magnitude, size, color, clustered, densityRank);
        }

        private static Mesh BuildMesh(IReadOnlyList<ProceduralStar> stars)
        {
            Vector3[] vertices = new Vector3[stars.Count * 4]; Color[] colors = new Color[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length]; Vector2[] data = new Vector2[vertices.Length]; Vector2[] density = new Vector2[vertices.Length]; int[] triangles = new int[stars.Count * 6];
            Vector2[] coordinates = { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            for (int index = 0; index < stars.Count; index++)
            {
                ProceduralStar star = stars[index]; int vertex = index * 4; float phase = Mathf.Repeat(index * 0.6180339f, 1f);
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices[vertex + corner] = star.Direction * Radius;
                    colors[vertex + corner] = new Color(star.Color.r, star.Color.g, star.Color.b, star.Magnitude);
                    uv[vertex + corner] = coordinates[corner]; data[vertex + corner] = new Vector2(phase, star.ApparentRadius);
                    density[vertex + corner] = new Vector2(star.DensityRank, star.Clustered ? 1f : 0f);
                }
                int triangle = index * 6; triangles[triangle] = vertex; triangles[triangle + 1] = vertex + 1; triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex; triangles[triangle + 4] = vertex + 2; triangles[triangle + 5] = vertex + 3;
            }
            Mesh mesh = new Mesh { name = "Procedural Star Field" }; mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.uv2 = data; mesh.uv3 = density;
            mesh.triangles = triangles; mesh.bounds = new Bounds(Vector3.zero, Vector3.one * Radius * 2.2f); mesh.UploadMeshData(true); return mesh;
        }

        private static Mesh BuildNebulaMesh(DeterministicRandom random)
        {
            const int count = 18; Vector3[] vertices = new Vector3[count * 4]; Color[] colors = new Color[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length]; Vector2[] data = new Vector2[vertices.Length]; int[] triangles = new int[count * 6];
            Vector2[] coordinates = { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            Color blue = new Color(0.08f, 0.26f, 0.82f, 0.20f); Color violet = new Color(0.68f, 0.10f, 0.82f, 0.18f);
            Color cyan = new Color(0.08f, 0.72f, 0.88f, 0.15f);
            for (int index = 0; index < count; index++)
            {
                Vector3 center = RandomDirection(random) * Radius; float size = Range(random, 95f, 240f);
                Color color = Color.Lerp(Color.Lerp(blue, violet, (float)random.NextUnitDouble()), cyan, (float)random.NextUnitDouble() * 0.38f);
                int vertex = index * 4; float phase = (float)random.NextUnitDouble();
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices[vertex + corner] = center; colors[vertex + corner] = color;
                    uv[vertex + corner] = coordinates[corner]; data[vertex + corner] = new Vector2(phase, size);
                }
                int triangle = index * 6; triangles[triangle] = vertex; triangles[triangle + 1] = vertex + 1; triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex; triangles[triangle + 4] = vertex + 2; triangles[triangle + 5] = vertex + 3;
            }
            Mesh mesh = new Mesh { name = "Procedural Nebula Field" }; mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.uv2 = data;
            mesh.triangles = triangles; mesh.bounds = new Bounds(Vector3.zero, Vector3.one * Radius * 2.2f); mesh.UploadMeshData(true); return mesh;
        }

        private static Vector3 RandomDirection(DeterministicRandom random)
        {
            float y = Range(random, -1f, 1f); float angle = Range(random, 0f, Mathf.PI * 2f); float radius = Mathf.Sqrt(1f - y * y);
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }
        private static Vector3 RandomInsideSphere(DeterministicRandom random) => RandomDirection(random) * Mathf.Pow((float)random.NextUnitDouble(), 1f / 3f);
        private static float Range(DeterministicRandom random, float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextUnitDouble());

        private float SampleLocalLuminosity(Vector3 observerPosition)
        {
            if (Time.unscaledTime >= _nextLightRefresh)
            {
                _sampledLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                _nextLightRefresh = Time.unscaledTime + 2f;
            }
            Color ambient = RenderSettings.ambientSkyColor * RenderSettings.ambientIntensity;
            float luminance = ColorLuminance(ambient);
            for (int index = 0; index < _sampledLights.Length; index++)
            {
                Light light = _sampledLights[index]; if (light == null || !light.isActiveAndEnabled || light.intensity <= 0f) continue;
                float influence = light.intensity * ColorLuminance(light.color);
                if (light.type != LightType.Directional)
                {
                    float distance = Vector3.Distance(observerPosition, light.transform.position);
                    if (distance >= light.range) continue;
                    float attenuation = 1f - distance / Mathf.Max(0.01f, light.range); influence *= attenuation * attenuation;
                    if (light.type == LightType.Spot)
                    {
                        Vector3 toObserver = (observerPosition - light.transform.position).normalized;
                        float cone = Mathf.InverseLerp(Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad), 1f, Vector3.Dot(light.transform.forward, toObserver));
                        influence *= cone;
                    }
                }
                luminance += influence;
            }
            return Mathf.Clamp01(luminance / CelestialCycleSystem.MaximumSolarIlluminationReference);
        }

        public static float ResolveDensityMultiplier(float luminosity)
        {
            if (luminosity <= 0.25f) return 30f;
            if (luminosity >= 0.75f) return 1f;
            float darkness = 1f - Mathf.InverseLerp(0.25f, 0.75f, luminosity);
            return 1f + 29f * darkness * darkness;
        }

        private static float ColorLuminance(Color color) => color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
    }
}
