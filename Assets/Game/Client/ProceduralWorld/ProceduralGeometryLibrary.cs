using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.ProceduralWorld
{
    public readonly struct ProceduralMeshKey : IEquatable<ProceduralMeshKey>
    {
        public ProceduralMeshKey(DecorationKind kind, uint visualAssetId, ProceduralVisualLod lod, byte variation, int styleVersion)
        {
            Kind = kind; VisualAssetId = visualAssetId; Lod = lod; Variation = variation; StyleVersion = styleVersion;
        }

        public DecorationKind Kind { get; }
        public uint VisualAssetId { get; }
        public ProceduralVisualLod Lod { get; }
        public byte Variation { get; }
        public int StyleVersion { get; }
        public bool Equals(ProceduralMeshKey other) => Kind == other.Kind && VisualAssetId == other.VisualAssetId && Lod == other.Lod && Variation == other.Variation && StyleVersion == other.StyleVersion;
        public override bool Equals(object obj) => obj is ProceduralMeshKey other && Equals(other);
        public override int GetHashCode() => ((((int)Kind * 397) ^ (int)VisualAssetId) * 397 ^ (int)Lod) * 397 ^ (Variation * 31) ^ StyleVersion;
        public override string ToString() => $"A{VisualAssetId}/{Kind}/LOD{(int)Lod}/V{Variation}/S{StyleVersion}";
    }

    public sealed class ProceduralMeshResource
    {
        public ProceduralMeshResource(Mesh mesh, ProceduralMeshKey key, bool ownsMesh = true, Material[] materials = null)
        {
            Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh)); Key = key; OwnsMesh = ownsMesh; Materials = materials;
        }

        public Mesh Mesh { get; }
        public ProceduralMeshKey Key { get; }
        public bool OwnsMesh { get; }
        public Material[] Materials { get; }
        public int VertexCount => Mesh.vertexCount;
        public int TriangleCount
        {
            get
            {
                int count = 0; for (int i = 0; i < Mesh.subMeshCount; i++) count += (int)Mesh.GetIndexCount(i) / 3; return count;
            }
        }
    }

    public sealed class ProceduralMeshCache : IDisposable
    {
        private readonly Dictionary<ProceduralMeshKey, ProceduralMeshResource> _resources = new Dictionary<ProceduralMeshKey, ProceduralMeshResource>();
        public int Count => _resources.Count;
        public bool TryGet(ProceduralMeshKey key, out ProceduralMeshResource resource) => _resources.TryGetValue(key, out resource);
        public void Add(ProceduralMeshResource resource) => _resources.Add(resource.Key, resource);
        public void Dispose()
        {
            foreach (ProceduralMeshResource resource in _resources.Values)
                if (resource.OwnsMesh && resource.Mesh != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(resource.Mesh);
                    else UnityEngine.Object.DestroyImmediate(resource.Mesh);
                }
            _resources.Clear();
        }
    }

    public interface IProceduralGeometryProvider
    {
        bool Supports(DecorationKind kind);
        ProceduralMeshResource Build(ProceduralMeshKey key, ProceduralStyleProfile style, ProceduralLodResolver lodResolver);
    }

    public sealed class NaturalDecorationGeometryProvider : IProceduralGeometryProvider
    {
        public bool Supports(DecorationKind kind) => kind == DecorationKind.Tree || kind == DecorationKind.Rock || kind == DecorationKind.Bush ||
            kind == DecorationKind.Flower || kind == DecorationKind.FlowerCluster || kind == DecorationKind.Mushroom ||
            kind == DecorationKind.MushroomCluster || kind == DecorationKind.TreeCluster || kind == DecorationKind.RockCluster ||
            kind == DecorationKind.BushCluster || kind == DecorationKind.ScaleMarker;

        public ProceduralMeshResource Build(ProceduralMeshKey key, ProceduralStyleProfile style, ProceduralLodResolver lodResolver)
        {
            int baseSegments = lodResolver.ResolveSegments(key.Lod);
            int segments = Mathf.Max(3, Mathf.RoundToInt(baseSegments * Mathf.Lerp(1.15f, 0.9f, style.Angularity)));
            long seed = SeedDerivation.Derive(key.StyleVersion, (uint)key.Kind, key.Variation + ((long)key.Lod << 16));
            DeterministicRandom random = new DeterministicRandom(seed);
            LowPolyMeshDraft draft = new LowPolyMeshDraft();
            switch (key.Kind)
            {
                case DecorationKind.Tree: ProceduralTreeGeometryBuilder.Build(draft, segments, key.Lod, key.Variation, style, random); break;
                case DecorationKind.Rock: ProceduralRockGeometryBuilder.Build(draft, key.Lod, key.Variation, style, random); break;
                case DecorationKind.Bush: BuildBush(draft, segments, key.Lod, key.Variation, style, random); break;
                case DecorationKind.Flower: ProceduralGroundFloraGeometryBuilder.BuildFlower(draft, key.Lod, key.Variation, random); break;
                case DecorationKind.FlowerCluster: ProceduralGroundFloraGeometryBuilder.BuildFlowerCluster(draft, key.Lod, key.Variation, random); break;
                case DecorationKind.Mushroom: ProceduralGroundFloraGeometryBuilder.BuildMushroom(draft, key.Lod, key.Variation, random); break;
                case DecorationKind.MushroomCluster: ProceduralGroundFloraGeometryBuilder.BuildMushroomCluster(draft, key.Lod, key.Variation, random); break;
                case DecorationKind.TreeCluster: ProceduralNaturalClusterGeometryBuilder.BuildTreeCluster(draft, segments, key.Lod, key.Variation, style, random); break;
                case DecorationKind.RockCluster: ProceduralNaturalClusterGeometryBuilder.BuildRockCluster(draft, key.Lod, key.Variation, style, random); break;
                case DecorationKind.BushCluster: ProceduralNaturalClusterGeometryBuilder.BuildBushCluster(draft, key.Lod, key.Variation, style, random); break;
                case DecorationKind.ScaleMarker: BuildMarker(draft, segments); break;
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
            return new ProceduralMeshResource(draft.CreateMesh($"Procedural {key}"), key);
        }

        private static void BuildBush(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, byte variation, ProceduralStyleProfile style, DeterministicRandom random)
        {
            float silhouette = Mathf.Lerp(0.06f, style.Asymmetry, style.SilhouetteVariation);
            float spread = variation == 1 ? 1.2f : variation == 3 ? 0.78f : 1f;
            draft.AddBipyramid(new Vector3(-0.25f * spread, 0.62f, 0f), 0.9f * spread, 0.75f, 0.48f, segments, 0, silhouette, random);
            if (lod != ProceduralVisualLod.Low)
                draft.AddBipyramid(new Vector3(0.48f * spread, 0.55f, 0.12f), 0.72f, 0.65f, 0.4f, segments, 1, silhouette, random);
            if (variation == 2 && lod == ProceduralVisualLod.High)
                draft.AddBipyramid(new Vector3(0.05f, 0.88f, -0.42f), 0.58f, 0.62f, 0.34f, segments, 1, silhouette, random);
        }

        private static void BuildMarker(LowPolyMeshDraft draft, int segments)
        {
            DeterministicRandom random = new DeterministicRandom(1);
            draft.AddPrism(Vector3.zero, 1.55f, 0.17f, 0.17f, segments, 0, 0f, random);
            draft.AddBipyramid(new Vector3(0f, 1.78f, 0f), 0.25f, 0.25f, 0.22f, segments, 0, 0f, random);
        }
    }

    internal static class ProceduralGroundFloraGeometryBuilder
    {
        public static void BuildFlower(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation, DeterministicRandom random)
        {
            float height = 0.72f + (variation % 3) * 0.09f;
            BuildFlowerAt(draft, Vector3.zero, height, 1f, lod, random);
        }

        public static void BuildFlowerCluster(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation, DeterministicRandom random)
        {
            int count = lod == ProceduralVisualLod.High ? 7 : lod == ProceduralVisualLod.Medium ? 5 : 3;
            for (int index = 0; index < count; index++)
            {
                float angle = index * 2.399963f + variation * 0.31f;
                float radius = index == 0 ? 0f : 0.18f + index * 0.055f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float scale = 0.72f + (float)random.NextUnitDouble() * 0.35f;
                BuildFlowerAt(draft, offset, 0.62f + scale * 0.22f, scale, lod, random);
            }
        }

        public static void BuildMushroom(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation, DeterministicRandom random)
        {
            BuildMushroomAt(draft, Vector3.zero, 0.72f + (variation % 3) * 0.1f, 1f, lod, random);
        }

        public static void BuildMushroomCluster(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation, DeterministicRandom random)
        {
            int count = lod == ProceduralVisualLod.High ? 6 : lod == ProceduralVisualLod.Medium ? 4 : 3;
            for (int index = 0; index < count; index++)
            {
                float angle = index * 2.399963f + variation * 0.47f;
                float radius = index == 0 ? 0f : 0.16f + index * 0.07f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float scale = 0.65f + (float)random.NextUnitDouble() * 0.5f;
                BuildMushroomAt(draft, offset, 0.58f + scale * 0.18f, scale, lod, random);
            }
        }

        private static void BuildFlowerAt(LowPolyMeshDraft draft, Vector3 offset, float height, float scale,
            ProceduralVisualLod lod, DeterministicRandom random)
        {
            int stemSegments = lod == ProceduralVisualLod.High ? 6 : 4;
            Vector3 top = offset + new Vector3(0.035f * scale, height * scale, -0.02f * scale);
            draft.AddTaperedBranch(offset, top, 0.045f * scale, 0.025f * scale, stemSegments, 0, 0.04f, random);
            int petals = lod == ProceduralVisualLod.Low ? 4 : 6;
            for (int petal = 0; petal < petals; petal++)
            {
                float angle = petal * Mathf.PI * 2f / petals;
                Vector3 center = top + new Vector3(Mathf.Cos(angle) * 0.13f, 0f, Mathf.Sin(angle) * 0.13f) * scale;
                draft.AddIrregularIcosphere(center, new Vector3(0.14f, 0.055f, 0.1f) * scale, 0,
                    1 + petal % 2, 0.08f, random);
            }
            draft.AddIrregularIcosphere(top + Vector3.up * 0.015f, Vector3.one * 0.085f * scale, 0, 2, 0.04f, random);
        }

        private static void BuildMushroomAt(LowPolyMeshDraft draft, Vector3 offset, float height, float scale,
            ProceduralVisualLod lod, DeterministicRandom random)
        {
            int stemSegments = lod == ProceduralVisualLod.High ? 7 : lod == ProceduralVisualLod.Medium ? 5 : 4;
            float stemHeight = height * scale * 0.62f;
            Vector3 capCenter = offset + Vector3.up * stemHeight;
            draft.AddTaperedBranch(offset, capCenter, 0.105f * scale, 0.075f * scale, stemSegments, 0, 0.06f, random);
            int detail = lod == ProceduralVisualLod.High ? 1 : 0;
            draft.AddIrregularIcosphere(capCenter + Vector3.up * 0.07f * scale,
                new Vector3(0.34f, 0.18f, 0.34f) * scale, detail, 1, 0.1f, random, 2);
        }
    }

    internal static class ProceduralRockGeometryBuilder
    {
        public static void Build(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation,
            ProceduralStyleProfile style, DeterministicRandom random)
        {
            int detail = lod == ProceduralVisualLod.High ? 1 : 0;
            float irregularity = Mathf.Lerp(0.16f, 0.34f, style.Asymmetry);
            switch (variation % 4)
            {
                case 0:
                    AddRock(draft, new Vector3(0f, 0.48f, 0f), new Vector3(1.25f, 0.78f, 1.02f), detail, irregularity, random);
                    break;
                case 1:
                    AddRock(draft, new Vector3(-0.06f, 0.76f, 0.02f), new Vector3(0.78f, 1.38f, 0.7f), detail, irregularity, random);
                    if (lod == ProceduralVisualLod.High)
                        AddRock(draft, new Vector3(0.42f, 0.24f, 0.18f), new Vector3(0.56f, 0.4f, 0.5f), 0, irregularity, random);
                    break;
                case 2:
                    AddRock(draft, new Vector3(-0.34f, 0.34f, 0.05f), new Vector3(0.86f, 0.62f, 0.78f), detail, irregularity, random);
                    AddRock(draft, new Vector3(0.48f, 0.28f, -0.16f), new Vector3(0.7f, 0.5f, 0.62f), lod == ProceduralVisualLod.High ? 1 : 0, irregularity, random);
                    if (lod != ProceduralVisualLod.Low)
                        AddRock(draft, new Vector3(0.08f, 0.22f, 0.58f), new Vector3(0.52f, 0.38f, 0.48f), 0, irregularity, random);
                    break;
                default:
                    AddRock(draft, new Vector3(0f, 0.27f, 0f), new Vector3(1.42f, 0.46f, 0.94f), detail, irregularity * 0.72f, random);
                    if (lod == ProceduralVisualLod.High)
                        AddRock(draft, new Vector3(-0.18f, 0.55f, -0.04f), new Vector3(0.92f, 0.28f, 0.68f), 0, irregularity, random);
                    break;
            }
        }

        private static void AddRock(LowPolyMeshDraft draft, Vector3 center, Vector3 radii, int subdivisions,
            float irregularity, DeterministicRandom random)
        {
            draft.AddIrregularIcosphere(center, radii, subdivisions, 0, irregularity, random, 3);
        }
    }

    internal static class ProceduralNaturalClusterGeometryBuilder
    {
        public static void BuildTreeCluster(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, byte variation,
            ProceduralStyleProfile style, DeterministicRandom random)
        {
            int count = lod == ProceduralVisualLod.High ? 5 : lod == ProceduralVisualLod.Medium ? 4 : 3;
            for (int index = 0; index < count; index++)
            {
                float angle = variation * 0.41f + index * 2.399963f;
                float radius = index == 0 ? 0f : 1.35f + index * 0.34f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * 0.72f);
                float scale = 0.68f + (float)random.NextUnitDouble() * 0.42f;
                LowPolyMeshDraft member = new LowPolyMeshDraft();
                ProceduralTreeGeometryBuilder.Build(member, segments, lod, (byte)((variation + index) % 4), style, random);
                draft.Append(member, offset, scale, angle * Mathf.Rad2Deg + 23f);
            }
        }

        public static void BuildRockCluster(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation,
            ProceduralStyleProfile style, DeterministicRandom random)
        {
            int count = lod == ProceduralVisualLod.High ? 6 : lod == ProceduralVisualLod.Medium ? 4 : 3;
            for (int index = 0; index < count; index++)
            {
                float angle = variation * 0.33f + index * 2.399963f;
                float radius = index == 0 ? 0f : 0.72f + index * 0.18f;
                LowPolyMeshDraft member = new LowPolyMeshDraft();
                ProceduralRockGeometryBuilder.Build(member, lod, (byte)((variation + index) % 4), style, random);
                float scale = 0.48f + (float)random.NextUnitDouble() * 0.46f;
                draft.Append(member, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * 0.7f), scale,
                    angle * Mathf.Rad2Deg);
            }
        }

        public static void BuildBushCluster(LowPolyMeshDraft draft, ProceduralVisualLod lod, byte variation,
            ProceduralStyleProfile style, DeterministicRandom random)
        {
            int count = lod == ProceduralVisualLod.High ? 7 : lod == ProceduralVisualLod.Medium ? 5 : 3;
            int detail = lod == ProceduralVisualLod.High ? 1 : 0;
            for (int index = 0; index < count; index++)
            {
                float angle = variation * 0.29f + index * 2.399963f;
                float radius = index == 0 ? 0f : 0.48f + index * 0.12f;
                Vector3 center = new Vector3(Mathf.Cos(angle) * radius, 0.45f, Mathf.Sin(angle) * radius * 0.72f);
                float scale = 0.56f + (float)random.NextUnitDouble() * 0.38f;
                draft.AddIrregularIcosphere(center, new Vector3(0.82f, 0.62f, 0.76f) * scale, detail,
                    index % 2, Mathf.Lerp(0.08f, style.Asymmetry, style.SilhouetteVariation), random);
            }
        }
    }

    internal static class ProceduralTreeGeometryBuilder
    {
        public static void Build(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, byte variation, ProceduralStyleProfile style, DeterministicRandom random)
        {
            float irregularity = Mathf.Lerp(0.08f, style.Asymmetry, style.SilhouetteVariation);
            switch (variation % 4)
            {
                case 0: BuildBroadleaf(draft, segments, lod, irregularity, random); break;
                case 1: BuildSlender(draft, segments, lod, irregularity, random); break;
                case 2: BuildConifer(draft, segments, lod, irregularity, random); break;
                default: BuildWindswept(draft, segments, lod, irregularity, random); break;
            }
        }

        private static void BuildBroadleaf(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, float irregularity, DeterministicRandom random)
        {
            BuildReferenceBroadleaf(draft, segments, lod, irregularity, random, 1f, 1f, 0f);
        }

        private static void BuildSlender(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, float irregularity, DeterministicRandom random)
        {
            BuildReferenceBroadleaf(draft, segments, lod, irregularity, random, 0.78f, 1.18f, -0.08f);
        }

        private static void BuildConifer(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, float irregularity, DeterministicRandom random)
        {
            BuildReferenceBroadleaf(draft, segments, lod, irregularity, random, 1.22f, 0.9f, 0.06f);
        }

        private static void BuildWindswept(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod, float irregularity, DeterministicRandom random)
        {
            BuildReferenceBroadleaf(draft, segments, lod, irregularity, random, 1.08f, 1f, 0.52f);
        }

        private static void BuildReferenceBroadleaf(LowPolyMeshDraft draft, int segments, ProceduralVisualLod lod,
            float irregularity, DeterministicRandom random, float widthScale, float heightScale, float lean)
        {
            Vector3 lower = new Vector3(lean * 0.28f, 2.15f * heightScale, 0.03f);
            Vector3 crownBase = new Vector3(lean, 3.15f * heightScale, 0f);
            draft.AddTaperedBranch(Vector3.zero, lower, 0.34f, 0.23f, segments, 0, 0.055f, random);
            draft.AddTaperedBranch(lower, crownBase, 0.23f, 0.12f, segments, 0, 0.05f, random);

            int branchSegments = Mathf.Max(4, segments - 2);
            if (lod != ProceduralVisualLod.Low)
            {
                draft.AddTaperedBranch(lower * 0.82f, crownBase + new Vector3(-1.05f * widthScale, 0.2f, 0.22f), 0.15f, 0.055f, branchSegments, 0, 0.045f, random);
                draft.AddTaperedBranch(lower * 0.9f, crownBase + new Vector3(0.96f * widthScale, 0.35f, -0.18f), 0.145f, 0.05f, branchSegments, 0, 0.045f, random);
            }
            if (lod == ProceduralVisualLod.High)
            {
                draft.AddTaperedBranch(crownBase * 0.78f, crownBase + new Vector3(-0.32f, 0.9f * heightScale, -0.72f * widthScale), 0.11f, 0.04f, branchSegments, 0, 0.04f, random);
                draft.AddTaperedBranch(crownBase * 0.82f, crownBase + new Vector3(0.46f, 1.08f * heightScale, 0.66f * widthScale), 0.1f, 0.035f, branchSegments, 0, 0.04f, random);
            }

            AddOrganicCrown(draft, crownBase + new Vector3(0f, 0.66f * heightScale, 0f), 1.78f * widthScale, 1.5f * heightScale, 1, 1, irregularity, random);
            AddOrganicCrown(draft, crownBase + new Vector3(-0.92f * widthScale, 0.42f * heightScale, 0.18f), 1.18f * widthScale, 1.04f * heightScale, lod == ProceduralVisualLod.High ? 1 : 0, 2, irregularity, random);
            if (lod != ProceduralVisualLod.Low)
                AddOrganicCrown(draft, crownBase + new Vector3(0.94f * widthScale, 0.5f * heightScale, -0.12f), 1.2f * widthScale, 1.08f * heightScale, lod == ProceduralVisualLod.High ? 1 : 0, 2, irregularity, random);
            if (lod != ProceduralVisualLod.Low)
            {
                int upperDetail = lod == ProceduralVisualLod.High ? 1 : 0;
                AddOrganicCrown(draft, crownBase + new Vector3(-0.22f, 1.38f * heightScale, -0.52f * widthScale), 1.08f * widthScale, 1.06f * heightScale, upperDetail, 1, irregularity, random);
                AddOrganicCrown(draft, crownBase + new Vector3(0.48f, 1.3f * heightScale, 0.5f * widthScale), 1.02f * widthScale, 1f * heightScale, upperDetail, 2, irregularity, random);
            }
        }

        private static void AddOrganicCrown(LowPolyMeshDraft draft, Vector3 center, float radius, float height,
            int subdivisions, int materialSlot, float irregularity, DeterministicRandom random)
        {
            draft.AddIrregularIcosphere(center, new Vector3(radius, height, radius * 0.9f), subdivisions,
                materialSlot, irregularity, random);
        }
    }

    public sealed class LowPolyMeshDraft
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<List<int>> _submeshes = new List<List<int>>();

        public void Append(LowPolyMeshDraft source, Vector3 offset, float scale, float yawDegrees)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            int vertexOffset = _vertices.Count;
            for (int index = 0; index < source._vertices.Count; index++)
            {
                _vertices.Add(offset + rotation * (source._vertices[index] * scale));
                _normals.Add(rotation * source._normals[index]);
                _colors.Add(source._colors[index]);
            }
            for (int slot = 0; slot < source._submeshes.Count; slot++)
            {
                while (_submeshes.Count <= slot) _submeshes.Add(new List<int>());
                List<int> sourceTriangles = source._submeshes[slot];
                for (int index = 0; index < sourceTriangles.Count; index++)
                    _submeshes[slot].Add(vertexOffset + sourceTriangles[index]);
            }
        }

        public void AddPrism(Vector3 center, float height, float bottomRadius, float topRadius, int segments, int materialSlot, float irregularity, DeterministicRandom random)
        {
            Vector3[] bottom = CreateRing(center, bottomRadius, segments, irregularity, random, 0f);
            Vector3[] top = CreateRing(center + Vector3.up * height, topRadius, segments, irregularity, random, 0.17f);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddQuad(bottom[i], top[i], top[next], bottom[next], materialSlot);
                AddTriangle(center, bottom[i], bottom[next], materialSlot);
                AddTriangle(center + Vector3.up * height, top[next], top[i], materialSlot);
            }
        }

        public void AddTaperedBranch(Vector3 start, Vector3 end, float startRadius, float endRadius, int segments, int materialSlot, float irregularity, DeterministicRandom random)
        {
            Vector3 axis = end - start;
            Vector3 direction = axis.normalized;
            Vector3 tangent = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.92f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(direction, tangent).normalized;
            Vector3[] startRing = new Vector3[segments];
            Vector3[] endRing = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float startDelta = 1f + ((float)random.NextUnitDouble() * 2f - 1f) * irregularity;
                float endDelta = 1f + ((float)random.NextUnitDouble() * 2f - 1f) * irregularity;
                Vector3 radial = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);
                startRing[i] = start + radial * startRadius * startDelta;
                endRing[i] = end + radial * endRadius * endDelta;
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddQuad(startRing[i], endRing[i], endRing[next], startRing[next], materialSlot);
                AddTriangle(start, startRing[next], startRing[i], materialSlot);
                AddTriangle(end, endRing[i], endRing[next], materialSlot);
            }
        }

        public void AddLayeredRadialVolume(Vector3 center, float radius, float height, float[] levels, float[] radiusProfile,
            int segments, int materialSlot, float irregularity, DeterministicRandom random)
        {
            if (levels == null || radiusProfile == null || levels.Length != radiusProfile.Length || levels.Length < 2)
                throw new ArgumentException("A layered volume requires matching profiles with at least two levels.");

            Vector3[][] rings = new Vector3[levels.Length][];
            Vector2 drift = Vector2.zero;
            for (int level = 0; level < levels.Length; level++)
            {
                drift += new Vector2(((float)random.NextUnitDouble() * 2f - 1f) * irregularity,
                    ((float)random.NextUnitDouble() * 2f - 1f) * irregularity) * radius * 0.18f;
                Vector3 ringCenter = center + new Vector3(drift.x, levels[level] * height, drift.y);
                rings[level] = CreateRing(ringCenter, radius * radiusProfile[level], segments, irregularity, random, level * 0.19f);
            }

            Vector3 bottom = center + Vector3.up * levels[0] * height;
            Vector3 top = center + Vector3.up * levels[levels.Length - 1] * height;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTriangle(bottom, rings[0][i], rings[0][next], materialSlot);
                for (int level = 0; level < rings.Length - 1; level++)
                    AddQuad(rings[level][i], rings[level + 1][i], rings[level + 1][next], rings[level][next], materialSlot);
                AddTriangle(top, rings[rings.Length - 1][next], rings[rings.Length - 1][i], materialSlot);
            }
        }

        public void AddIrregularIcosphere(Vector3 center, Vector3 radii, int subdivisions, int materialSlot,
            float irregularity, DeterministicRandom random, int materialVariationSlots = 1)
        {
            float golden = (1f + Mathf.Sqrt(5f)) * 0.5f;
            List<Vector3> points = new List<Vector3>
            {
                new Vector3(-1, golden, 0), new Vector3(1, golden, 0), new Vector3(-1, -golden, 0), new Vector3(1, -golden, 0),
                new Vector3(0, -1, golden), new Vector3(0, 1, golden), new Vector3(0, -1, -golden), new Vector3(0, 1, -golden),
                new Vector3(golden, 0, -1), new Vector3(golden, 0, 1), new Vector3(-golden, 0, -1), new Vector3(-golden, 0, 1)
            };
            List<int> faces = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            for (int step = 0; step < Mathf.Clamp(subdivisions, 0, 1); step++)
            {
                Dictionary<long, int> midpointCache = new Dictionary<long, int>();
                List<int> refined = new List<int>(faces.Count * 4);
                for (int face = 0; face < faces.Count; face += 3)
                {
                    int a = faces[face]; int b = faces[face + 1]; int c = faces[face + 2];
                    int ab = GetMidpoint(points, midpointCache, a, b);
                    int bc = GetMidpoint(points, midpointCache, b, c);
                    int ca = GetMidpoint(points, midpointCache, c, a);
                    refined.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = refined;
            }

            for (int index = 0; index < points.Count; index++)
            {
                Vector3 direction = points[index].normalized;
                float variation = 1f + ((float)random.NextUnitDouble() * 2f - 1f) * irregularity;
                points[index] = center + Vector3.Scale(direction, radii) * variation;
            }
            for (int face = 0; face < faces.Count; face += 3)
            {
                int slot = materialSlot + ((face / 3) % Mathf.Max(1, materialVariationSlots));
                AddTriangle(points[faces[face]], points[faces[face + 1]], points[faces[face + 2]], slot);
            }
        }

        private static int GetMidpoint(List<Vector3> points, Dictionary<long, int> cache, int left, int right)
        {
            int minimum = Mathf.Min(left, right); int maximum = Mathf.Max(left, right);
            long key = ((long)minimum << 32) | (uint)maximum;
            if (cache.TryGetValue(key, out int existing)) return existing;
            int index = points.Count;
            points.Add((points[left] + points[right]).normalized);
            cache.Add(key, index);
            return index;
        }

        public void AddBipyramid(Vector3 center, float radius, float topHeight, float bottomDepth, int segments, int materialSlot, float irregularity, DeterministicRandom random)
        {
            Vector3[] ring = CreateRing(center, radius, segments, irregularity, random, 0f);
            Vector3 top = center + Vector3.up * topHeight; Vector3 bottom = center - Vector3.up * bottomDepth;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTriangle(top, ring[next], ring[i], materialSlot);
                AddTriangle(bottom, ring[i], ring[next], materialSlot);
            }
        }

        public void AddIrregularVolume(Vector3 center, float radius, float height, int segments, int materialSlot, float irregularity, DeterministicRandom random)
        {
            Vector3[] ring = CreateRing(center, radius, segments, Mathf.Max(0.16f, irregularity), random, 0.13f);
            Vector3 top = center + new Vector3(0.12f, height, -0.08f); Vector3 bottom = center - Vector3.up * height * 0.48f;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTriangle(top, ring[next], ring[i], materialSlot);
                AddTriangle(bottom, ring[i], ring[next], materialSlot);
            }
        }

        public Mesh CreateMesh(string name)
        {
            Mesh mesh = new Mesh { name = name, indexFormat = _vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(_vertices); mesh.SetNormals(_normals); mesh.SetColors(_colors); mesh.subMeshCount = _submeshes.Count;
            for (int i = 0; i < _submeshes.Count; i++) mesh.SetTriangles(_submeshes[i], i, false);
            mesh.RecalculateBounds(); return mesh;
        }

        private Vector3[] CreateRing(Vector3 center, float radius, int segments, float irregularity, DeterministicRandom random, float phase)
        {
            Vector3[] ring = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = phase + i * Mathf.PI * 2f / segments;
                float delta = 1f + (((float)random.NextUnitDouble() * 2f - 1f) * irregularity);
                ring[i] = center + new Vector3(Mathf.Cos(angle) * radius * delta, 0f, Mathf.Sin(angle) * radius * delta);
            }
            return ring;
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int slot)
        {
            AddTriangle(a, d, b, slot); AddTriangle(d, c, b, slot);
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, int slot)
        {
            while (_submeshes.Count <= slot) _submeshes.Add(new List<int>());
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized; int start = _vertices.Count;
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
            _normals.Add(normal); _normals.Add(normal); _normals.Add(normal);
            _colors.Add(Color.white); _colors.Add(Color.white); _colors.Add(Color.white);
            _submeshes[slot].Add(start); _submeshes[slot].Add(start + 1); _submeshes[slot].Add(start + 2);
        }
    }
}
