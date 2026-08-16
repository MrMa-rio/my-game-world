using MyGameWorld.Client.ProceduralWorld;
using MyGameWorld.Shared.World;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ProceduralRuntimeGeometryTests
    {
        [TestCase(DecorationKind.Tree)]
        [TestCase(DecorationKind.Rock)]
        [TestCase(DecorationKind.Bush)]
        public void Build_SameKey_ProducesIdenticalGeometry(DecorationKind kind)
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            ProceduralMeshKey key = new ProceduralMeshKey(kind, WorldVisualAssetIds.ForDecoration(kind).Value, ProceduralVisualLod.High, 2, 1);
            ProceduralMeshResource first = provider.Build(key, new ProceduralStyleProfile(), new ProceduralLodResolver());
            ProceduralMeshResource second = provider.Build(key, new ProceduralStyleProfile(), new ProceduralLodResolver());
            try
            {
                Assert.That(second.Mesh.vertices, Is.EqualTo(first.Mesh.vertices));
                Assert.That(second.Mesh.triangles, Is.EqualTo(first.Mesh.triangles));
                Assert.That(second.TriangleCount, Is.EqualTo(first.TriangleCount));
            }
            finally
            {
                Object.DestroyImmediate(first.Mesh); Object.DestroyImmediate(second.Mesh);
            }
        }

        [TestCase(DecorationKind.Tree)]
        [TestCase(DecorationKind.Rock)]
        [TestCase(DecorationKind.Bush)]
        public void Build_LowerLod_ReducesGeometry(DecorationKind kind)
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            uint assetId = WorldVisualAssetIds.ForDecoration(kind).Value;
            ProceduralMeshResource high = provider.Build(new ProceduralMeshKey(kind, assetId, ProceduralVisualLod.High, 0, 1), new ProceduralStyleProfile(), new ProceduralLodResolver());
            ProceduralMeshResource low = provider.Build(new ProceduralMeshKey(kind, assetId, ProceduralVisualLod.Low, 0, 1), new ProceduralStyleProfile(), new ProceduralLodResolver());
            try
            {
                Assert.That(low.VertexCount, Is.LessThan(high.VertexCount));
                Assert.That(low.TriangleCount, Is.LessThan(high.TriangleCount));
            }
            finally
            {
                Object.DestroyImmediate(high.Mesh); Object.DestroyImmediate(low.Mesh);
            }
        }

        [Test]
        public void Build_TreeArchetypes_ProduceDistinctDeterministicSilhouettes()
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            ProceduralStyleProfile style = new ProceduralStyleProfile();
            ProceduralLodResolver lodResolver = new ProceduralLodResolver();
            Mesh[] meshes = new Mesh[4];
            try
            {
                for (byte variation = 0; variation < meshes.Length; variation++)
                {
                    ProceduralMeshKey key = new ProceduralMeshKey(DecorationKind.Tree,
                        WorldVisualAssetIds.TemperateTree.Value, ProceduralVisualLod.High, variation, style.StyleVersion);
                    meshes[variation] = provider.Build(key, style, lodResolver).Mesh;
                    Assert.That(meshes[variation].subMeshCount, Is.EqualTo(4));
                    Assert.That(meshes[variation].bounds.size.y, Is.GreaterThan(3f));
                    Assert.That(HasOutwardFacingTriangle(meshes[variation], Vector3.back), Is.True);
                }

                for (int left = 0; left < meshes.Length; left++)
                    for (int right = left + 1; right < meshes.Length; right++)
                        Assert.That(meshes[right].vertices, Is.Not.EqualTo(meshes[left].vertices));
            }
            finally
            {
                for (int index = 0; index < meshes.Length; index++)
                    if (meshes[index] != null) Object.DestroyImmediate(meshes[index]);
            }
        }

        [Test]
        public void Resolve_TreeAtSandboxOverview_PreservesHighLod()
        {
            ZoneDNA zone = new ZoneDNA(new ZoneId(1), 42, BiomeId.TemperateGrassland,
                TerrainProfileId.RollingLowPoly, TerrainGeneratorV2.GeneratorVersion, new AssetCatalogVersion(1));
            DecorationPlacement tree = new DecorationPlacement(new WorldElementId(1), zone, 1,
                DecorationKind.Tree, WorldVisualAssetIds.TemperateTree, new WorldVector3(0f, 0f, 0f), 0f, 1f);

            ProceduralVisualLod lod = new ProceduralLodResolver().Resolve(tree, new Vector3(0f, 210f, -330f));

            Assert.That(lod, Is.EqualTo(ProceduralVisualLod.High));
        }

        [Test]
        public void Build_RockFamilies_ProduceDistinctThreeToneGeometry()
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            ProceduralStyleProfile style = new ProceduralStyleProfile();
            ProceduralLodResolver lodResolver = new ProceduralLodResolver();
            Mesh[] meshes = new Mesh[4];
            try
            {
                for (byte variation = 0; variation < meshes.Length; variation++)
                {
                    ProceduralMeshKey key = new ProceduralMeshKey(DecorationKind.Rock,
                        WorldVisualAssetIds.TemperateRock.Value, ProceduralVisualLod.High, variation, style.StyleVersion);
                    ProceduralMeshResource resource = provider.Build(key, style, lodResolver);
                    meshes[variation] = resource.Mesh;
                    Assert.That(resource.Mesh.subMeshCount, Is.EqualTo(3));
                    Assert.That(resource.TriangleCount, Is.InRange(80, 300));
                }
                for (int left = 0; left < meshes.Length; left++)
                    for (int right = left + 1; right < meshes.Length; right++)
                        Assert.That(meshes[right].vertices, Is.Not.EqualTo(meshes[left].vertices));
            }
            finally
            {
                for (int index = 0; index < meshes.Length; index++)
                    if (meshes[index] != null) Object.DestroyImmediate(meshes[index]);
            }
        }

        [TestCase(DecorationKind.Flower)]
        [TestCase(DecorationKind.FlowerCluster)]
        [TestCase(DecorationKind.Mushroom)]
        [TestCase(DecorationKind.MushroomCluster)]
        public void Build_GroundFlora_IsDeterministicAndUsesThreeToneMaterialSlots(DecorationKind kind)
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            ProceduralStyleProfile style = new ProceduralStyleProfile();
            ProceduralLodResolver lodResolver = new ProceduralLodResolver();
            ProceduralMeshKey key = new ProceduralMeshKey(kind, WorldVisualAssetIds.ForDecoration(kind).Value,
                ProceduralVisualLod.High, 2, style.StyleVersion);
            ProceduralMeshResource first = provider.Build(key, style, lodResolver);
            ProceduralMeshResource second = provider.Build(key, style, lodResolver);
            try
            {
                Assert.That(first.Mesh.vertices, Is.EqualTo(second.Mesh.vertices));
                Assert.That(first.Mesh.triangles, Is.EqualTo(second.Mesh.triangles));
                Assert.That(first.Mesh.subMeshCount, Is.EqualTo(3));
                Assert.That(first.VertexCount, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(first.Mesh);
                Object.DestroyImmediate(second.Mesh);
            }
        }

        [TestCase(DecorationKind.Flower)]
        [TestCase(DecorationKind.FlowerCluster)]
        [TestCase(DecorationKind.Mushroom)]
        [TestCase(DecorationKind.MushroomCluster)]
        public void Build_GroundFloraHighLod_HasMoreGeometryThanLowLod(DecorationKind kind)
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            ProceduralStyleProfile style = new ProceduralStyleProfile();
            ProceduralLodResolver lodResolver = new ProceduralLodResolver();
            ProceduralMeshResource high = provider.Build(new ProceduralMeshKey(kind, WorldVisualAssetIds.ForDecoration(kind).Value,
                ProceduralVisualLod.High, 1, style.StyleVersion), style, lodResolver);
            ProceduralMeshResource low = provider.Build(new ProceduralMeshKey(kind, WorldVisualAssetIds.ForDecoration(kind).Value,
                ProceduralVisualLod.Low, 1, style.StyleVersion), style, lodResolver);
            try { Assert.That(high.VertexCount, Is.GreaterThan(low.VertexCount)); }
            finally { Object.DestroyImmediate(high.Mesh); Object.DestroyImmediate(low.Mesh); }
        }

        [TestCase(DecorationKind.TreeCluster)]
        [TestCase(DecorationKind.RockCluster)]
        [TestCase(DecorationKind.BushCluster)]
        public void Build_NaturalCluster_IsOneDeterministicMeshWithLodReduction(DecorationKind kind)
        {
            NaturalDecorationGeometryProvider provider = new NaturalDecorationGeometryProvider();
            ProceduralStyleProfile style = new ProceduralStyleProfile();
            ProceduralLodResolver resolver = new ProceduralLodResolver();
            ProceduralMeshKey highKey = new ProceduralMeshKey(kind, WorldVisualAssetIds.ForDecoration(kind).Value,
                ProceduralVisualLod.High, 2, style.StyleVersion);
            ProceduralMeshResource first = provider.Build(highKey, style, resolver);
            ProceduralMeshResource second = provider.Build(highKey, style, resolver);
            ProceduralMeshResource low = provider.Build(new ProceduralMeshKey(kind, WorldVisualAssetIds.ForDecoration(kind).Value,
                ProceduralVisualLod.Low, 2, style.StyleVersion), style, resolver);
            try
            {
                Assert.That(first.Mesh.vertices, Is.EqualTo(second.Mesh.vertices));
                Assert.That(first.Mesh.triangles, Is.EqualTo(second.Mesh.triangles));
                Assert.That(first.VertexCount, Is.GreaterThan(low.VertexCount));
                Assert.That(first.Mesh.bounds.size.x, Is.GreaterThan(1.5f));
            }
            finally
            {
                Object.DestroyImmediate(first.Mesh); Object.DestroyImmediate(second.Mesh); Object.DestroyImmediate(low.Mesh);
            }
        }

        [Test]
        public void ResolveTerrainArt_FlatTrianglesReceiveDeterministicUniformFaceColor()
        {
            WorldVector3[] vertices =
            {
                new WorldVector3(0,0,0), new WorldVector3(0,0,1), new WorldVector3(1,0,0),
                new WorldVector3(1,0,0), new WorldVector3(0,0,1), new WorldVector3(1,0,1)
            };
            WorldVector3[] normals = { new WorldVector3(0,1,0), new WorldVector3(0,1,0), new WorldVector3(0,1,0), new WorldVector3(0,1,0), new WorldVector3(0,1,0), new WorldVector3(0,1,0) };
            WorldColor[] source = { new WorldColor(.2f,.5f,.2f), new WorldColor(.3f,.6f,.2f), new WorldColor(.2f,.55f,.25f), new WorldColor(.3f,.6f,.2f), new WorldColor(.2f,.55f,.25f), new WorldColor(.35f,.65f,.25f) };
            TerrainChunkData data = new TerrainChunkData(2, 3, vertices, normals, source, new[] { 0, 1, 2, 3, 4, 5 });

            Color[] first = ProceduralTerrainSurfaceArtResolver.Resolve(data, 6);
            Color[] second = ProceduralTerrainSurfaceArtResolver.Resolve(data, 6);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first[0], Is.EqualTo(first[1]));
            Assert.That(first[1], Is.EqualTo(first[2]));
            Assert.That(first[3], Is.EqualTo(first[4]));
            Assert.That(first[4], Is.EqualTo(first[5]));
        }

        private static bool HasOutwardFacingTriangle(Mesh mesh, Vector3 viewDirection)
        {
            Vector3[] normals = mesh.normals;
            for (int index = 0; index < normals.Length; index++)
                if (Vector3.Dot(normals[index], viewDirection) > 0.35f) return true;
            return false;
        }

        [Test]
        public void Request_RegisteredPrefabAsset_UsesFiniteMeshWithoutTakingOwnership()
        {
            Mesh finiteMesh = new Mesh { name = "Finite Registry Mesh" };
            finiteMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            finiteMesh.triangles = new[] { 0, 1, 2 };
            finiteMesh.RecalculateNormals();
            GameObject prefab = new GameObject("Finite Tree Prefab");
            prefab.AddComponent<MeshFilter>().sharedMesh = finiteMesh;
            prefab.AddComponent<MeshRenderer>();
            GameObject host = new GameObject("Runtime Manager Test");
            ProceduralWorldMaterialLibrary materials = new ProceduralWorldMaterialLibrary();
            try
            {
                AssetCatalogVersion catalogVersion = new AssetCatalogVersion(1);
                ProceduralRuntimeManager manager = host.AddComponent<ProceduralRuntimeManager>();
                manager.Initialize(materials, new SingleAssetRegistry(catalogVersion, WorldVisualAssetIds.TemperateTree, prefab));
                manager.SetInstanceParent(host.transform);
                ZoneDNA zone = new ZoneDNA(new ZoneId(1), 42, BiomeId.TemperateGrassland,
                    TerrainProfileId.RollingLowPoly, TerrainGeneratorV2.GeneratorVersion, catalogVersion);
                DecorationPlacement definition = new DecorationPlacement(new WorldElementId(10001), zone, 77,
                    DecorationKind.Tree, WorldVisualAssetIds.TemperateTree, new WorldVector3(0f, 0f, 0f), 0f, 1f);
                manager.Request(new ProceduralGenerationRequest(definition,
                    new ProceduralEnvironmentContext(BiomeId.TemperateGrassland, Vector3.up, 0f, 0f),
                    ProceduralVisualLod.High, GenerationPriority.High));
                manager.FlushQueue();

                WorldElementRuntimeIdentity identity = host.GetComponentInChildren<WorldElementRuntimeIdentity>();
                Assert.That(identity, Is.Not.Null);
                Assert.That(identity.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(finiteMesh));
                Assert.That(manager.Metrics.GeneratedMeshes, Is.Zero);
                Assert.That(manager.Metrics.ResolvedFiniteAssets, Is.EqualTo(1));
                Object.DestroyImmediate(identity.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Assert.That(finiteMesh, Is.Not.Null);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(finiteMesh);
                materials.Dispose();
            }
        }

        private sealed class SingleAssetRegistry : IAssetRegistry<UnityEngine.Object>
        {
            private readonly AssetId _id;
            private readonly UnityEngine.Object _asset;
            public SingleAssetRegistry(AssetCatalogVersion version, AssetId id, UnityEngine.Object asset) { Version = version; _id = id; _asset = asset; }
            public AssetCatalogVersion Version { get; }
            public bool TryResolve(AssetId assetId, out UnityEngine.Object asset)
            {
                asset = assetId == _id ? _asset : null;
                return asset != null;
            }
        }
    }
}
