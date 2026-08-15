using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class WorldGenerationTests
    {
        [Test]
        public void Constructor_DefaultBudget_ResolvesExpectedGeometryCounts()
        {
            TerrainGenerationConfig config = TerrainGenerationConfig.CreateSandboxDefault();

            Assert.That(config.ResolvedResolution, Is.EqualTo(201));
            Assert.That(config.LogicalVertexCount, Is.EqualTo(40401));
            Assert.That(config.TriangleCount, Is.EqualTo(80000));
            Assert.That(config.TriangleCount, Is.LessThanOrEqualTo(config.TargetTriangleBudget));
            Assert.That(config.CellsPerChunk, Is.EqualTo(20));
        }

        [Test]
        public void Generate_SameSeed_ProducesSameFingerprintAndPlacements()
        {
            ZoneGenerationResult first = Generate(100);
            ZoneGenerationResult second = Generate(100);

            Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
            Assert.That(second.Terrain.Fingerprint, Is.EqualTo(first.Terrain.Fingerprint));
            Assert.That(second.Decorations, Is.EqualTo(first.Decorations));
        }

        [Test]
        public void Generate_DifferentSeed_ProducesDifferentFingerprint()
        {
            ZoneGenerationResult first = Generate(100);
            ZoneGenerationResult second = Generate(101);

            Assert.That(second.Fingerprint, Is.Not.EqualTo(first.Fingerprint));
            Assert.That(second.Terrain.Fingerprint, Is.Not.EqualTo(first.Terrain.Fingerprint));
        }

        [Test]
        public void Generate_DefaultSandboxSeed_MatchesVersionTwoGoldenFingerprint()
        {
            ZoneGenerationResult result = Generate(829172);

            Assert.That(result.Fingerprint, Is.EqualTo(0xD182ABC90DEBAE26UL));
        }

        [Test]
        public void Generate_FlatShading_ProducesBudgetedValidTopology()
        {
            ZoneGenerationResult result = Generate(100);

            Assert.That(result.Terrain.Chunks.Count, Is.EqualTo(100));
            Assert.That(result.Terrain.TriangleCount, Is.EqualTo(80000));
            Assert.That(result.Terrain.RenderedVertexCount, Is.EqualTo(240000));
            foreach (TerrainChunkData chunk in result.Terrain.Chunks)
            {
                Assert.That(chunk.TriangleCount, Is.EqualTo(800));
                Assert.That(chunk.VertexCount, Is.EqualTo(2400));
                for (int index = 0; index < chunk.Triangles.Length; index++)
                {
                    Assert.That(chunk.Triangles[index], Is.InRange(0, chunk.VertexCount - 1));
                }

                for (int index = 0; index < chunk.Normals.Length; index++)
                {
                    Assert.That(chunk.Normals[index].Y, Is.GreaterThan(0f));
                }
            }
        }

        [Test]
        public void Generate_AdjacentSmoothChunks_ShareIdenticalBorder()
        {
            TerrainGenerationConfig config = CreateConfig(TerrainShadingMode.Smooth);
            ZoneGenerationResult result = Generate(100, config);
            TerrainChunkData left = result.Terrain.Chunks[0];
            TerrainChunkData right = result.Terrain.Chunks[1];
            float sharedBorder = (-config.Width * 0.5f) + (config.CellsPerChunk * (config.Width / (config.ResolvedResolution - 1)));
            List<WorldVector3> leftBorder = FindBorder(left.Vertices, sharedBorder);
            List<WorldVector3> rightBorder = FindBorder(right.Vertices, sharedBorder);

            leftBorder.Sort((first, second) => first.Z.CompareTo(second.Z));
            rightBorder.Sort((first, second) => first.Z.CompareTo(second.Z));
            Assert.That(leftBorder.Count, Is.EqualTo(config.CellsPerChunk + 1));
            Assert.That(rightBorder, Has.Count.EqualTo(leftBorder.Count));
            for (int index = 0; index < leftBorder.Count; index++)
            {
                Assert.That(rightBorder[index], Is.EqualTo(leftBorder[index]));
            }
        }

        [Test]
        public void Generate_TemperateGrassland_ContainsLandformAndPathVariation()
        {
            ZoneGenerationResult result = Generate(100);
            float[] heights = result.Terrain.HeightField.CopyHeights();
            float[] paths = result.Terrain.HeightField.CopyPathMasks();
            float minimum = float.MaxValue;
            float maximum = float.MinValue;
            int pathSamples = 0;

            for (int index = 0; index < heights.Length; index++)
            {
                minimum = Math.Min(minimum, heights[index]);
                maximum = Math.Max(maximum, heights[index]);
                if (paths[index] > 0.5f)
                {
                    pathSamples++;
                }
            }

            Assert.That(maximum - minimum, Is.GreaterThan(4f));
            Assert.That(pathSamples, Is.GreaterThan(0));
        }

        [Test]
        public void Generate_Decorations_RespectMinimumDistanceAndTerrainRules()
        {
            ZoneGenerationResult result = Generate(100);
            float minimumDistance = BiomeDefinition.CreateExpandedTemperateGrassland().MinimumDecorationDistance;
            IReadOnlyList<DecorationPlacement> placements = result.Decorations;

            Assert.That(placements.Count, Is.GreaterThan(12));
            for (int firstIndex = 0; firstIndex < placements.Count; firstIndex++)
            {
                DecorationPlacement first = placements[firstIndex];
                Assert.That(first.Position.Y, Is.EqualTo(result.Terrain.HeightField.SampleHeight(first.Position.X, first.Position.Z)).Within(0.0001f));
                if (first.Kind == DecorationKind.ScaleMarker)
                {
                    continue;
                }

                Assert.That(result.Terrain.HeightField.SamplePathMask(first.Position.X, first.Position.Z), Is.LessThanOrEqualTo(0.34f));
                for (int secondIndex = firstIndex + 1; secondIndex < placements.Count; secondIndex++)
                {
                    DecorationPlacement second = placements[secondIndex];
                    if (second.Kind == DecorationKind.ScaleMarker)
                    {
                        continue;
                    }

                    float deltaX = first.Position.X - second.Position.X;
                    float deltaZ = first.Position.Z - second.Position.Z;
                    float distance = (float)Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
                    Assert.That(distance, Is.GreaterThanOrEqualTo(minimumDistance));
                }
            }
        }

        [Test]
        public void Generate_ElementsHaveIndependentIdentitySeedVersionAndBounds()
        {
            ZoneGenerationResult result = Generate(100);
            HashSet<long> ids = new HashSet<long>();
            Assert.That(ids.Add(result.Features.Terrain.ElementId.Value), Is.True);
            Assert.That(result.Features.Terrain.ElementKind, Is.EqualTo(WorldElementKind.TerrainSurface));
            foreach (LandformDNA feature in result.Features.Landforms)
            {
                Assert.That(ids.Add(feature.ElementId.Value), Is.True);
                Assert.That(feature.Seed, Is.Not.EqualTo(result.DNA.Seed));
                Assert.That(feature.GeneratorVersion, Is.EqualTo(result.DNA.GeneratorVersion));
                Assert.That(feature.Bounds.Radius, Is.GreaterThan(0f));
            }
            foreach (PathDNA path in result.Features.Paths) Assert.That(ids.Add(path.ElementId.Value), Is.True);
            foreach (DecorationPlacement decoration in result.Decorations)
            {
                Assert.That(ids.Add(decoration.ElementId.Value), Is.True);
                Assert.That(decoration.Seed, Is.Not.EqualTo(0));
                Assert.That(decoration.VisualAssetId, Is.EqualTo(WorldVisualAssetIds.ForDecoration(decoration.Kind)));
                Assert.That(decoration.ShapeA, Is.GreaterThan(0f));
            }
            Assert.That(result.Features.Landforms.Count, Is.LessThanOrEqualTo(WorldGenerationLimits.Sandbox.MaxLandforms));
            Assert.That(result.Features.Paths.Count, Is.LessThanOrEqualTo(WorldGenerationLimits.Sandbox.MaxPaths));
            Assert.That(result.Decorations.Count, Is.LessThanOrEqualTo(WorldGenerationLimits.Sandbox.MaxDecorations));
        }

        [Test]
        public void ResolveTerrainContact_ReturnsSurfaceAndInfluencingFeature()
        {
            ZoneGenerationResult result = Generate(100);
            LandformDNA feature = result.Features.Landforms[0];
            IReadOnlyList<WorldElementDNA> contact = result.ResolveTerrainContact(feature.Bounds.CenterX, feature.Bounds.CenterZ);
            Assert.That(contact[0], Is.SameAs(result.Features.Terrain));
            Assert.That(contact, Does.Contain(feature));
        }

        private static ZoneGenerationResult Generate(long seed, TerrainGenerationConfig config = null)
        {
            TerrainGenerationConfig resolvedConfig = config ?? TerrainGenerationConfig.CreateSandboxDefault();
            BiomeDefinition biome = BiomeDefinition.CreateExpandedTemperateGrassland();
            ZoneDNA dna = new ZoneDNA(
                new ZoneId(1),
                seed,
                BiomeId.TemperateGrassland,
                TerrainProfileId.RollingLowPoly,
                TerrainGeneratorV2.GeneratorVersion,
                new AssetCatalogVersion(1));
            return new ZoneGeneratorV2(resolvedConfig, biome).Generate(dna);
        }

        private static TerrainGenerationConfig CreateConfig(TerrainShadingMode shadingMode)
        {
            return new TerrainGenerationConfig(1000f, 1000f, 257, 40f, 80000, 10, 10, shadingMode);
        }

        private static List<WorldVector3> FindBorder(IEnumerable<WorldVector3> vertices, float x)
        {
            List<WorldVector3> result = new List<WorldVector3>();
            foreach (WorldVector3 vertex in vertices)
            {
                if (Math.Abs(vertex.X - x) < 0.0001f)
                {
                    result.Add(vertex);
                }
            }

            return result;
        }
    }
}
