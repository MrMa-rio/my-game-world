using System.Linq;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class DistantWorldArchitectureTests
    {
        [Test]
        public void Sample_SameCoordinateAndBands_IsDeterministic()
        {
            HierarchicalWorldGenerator generator = new HierarchicalWorldGenerator(829174, 4);
            HierarchicalHeightSample first = generator.Sample(18400d, 32100d, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro);
            HierarchicalHeightSample second = generator.Sample(18400d, 32100d, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro);
            Assert.That(second.Height, Is.EqualTo(first.Height));
            Assert.That(second.Color, Is.EqualTo(first.Color));
        }

        [Test]
        public void Sample_ProgressiveFrequencyBands_PreserveMacroMountain()
        {
            HierarchicalWorldGenerator generator = new HierarchicalWorldGenerator(829174, 4);
            HierarchicalHeightSample horizon = generator.Sample(18400d, 32100d, TerrainFrequencyBand.Macro);
            HierarchicalHeightSample near = generator.Sample(18400d, 32100d, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro);
            Assert.That(near.Macro, Is.EqualTo(horizon.Macro));
            Assert.That(horizon.Height, Is.EqualTo(horizon.Macro));
            Assert.That(near.Height, Is.EqualTo(near.Macro + near.Meso + near.Micro).Within(0.001f));
        }

        [Test]
        public void Resolve_NearBoundary_UsesHysteresis()
        {
            WorldRepresentationProfile profile = WorldRepresentationProfile.CreateDefault();
            Assert.That(profile.Resolve(2050d, WorldRepresentationKind.Near), Is.EqualTo(WorldRepresentationKind.Near));
            Assert.That(profile.Resolve(2300d, WorldRepresentationKind.Near), Is.EqualTo(WorldRepresentationKind.Medium));
        }

        [Test]
        public void Select_ViewerAtOrigin_UsesMultipleSpatialScalesWithoutOverlapAtCenters()
        {
            WorldSpatialHierarchy hierarchy = new WorldSpatialHierarchy();
            var cells = hierarchy.Select(new GlobalPosition(0d, 0d, 0d), WorldRepresentationProfile.CreateDefault());
            Assert.That(cells.Count, Is.GreaterThan(20));
            Assert.That(cells.Any(cell => cell.Representation == WorldRepresentationKind.Horizon), Is.True);
            Assert.That(cells.Any(cell => cell.Representation == WorldRepresentationKind.Near || cell.Representation == WorldRepresentationKind.Simulation), Is.True);
            Assert.That(cells.GroupBy(cell => cell.Coordinate).All(group => group.Count() == 1), Is.True);
        }

        [Test]
        public void Build_NeighborCells_SharesExactBorderHeights()
        {
            HierarchicalWorldGenerator generator = new HierarchicalWorldGenerator(99, 4);
            WorldRepresentationLevel level = WorldRepresentationProfile.CreateDefault()[WorldRepresentationKind.Far];
            TerrainRepresentationBuilder builder = new TerrainRepresentationBuilder();
            WorldCell left = new WorldCell(new WorldCellCoordinate(0, 0, 0), new WorldBounds(0d, 0d, 2048d), WorldRepresentationKind.Far);
            WorldCell right = new WorldCell(new WorldCellCoordinate(1, 0, 0), new WorldBounds(2048d, 0d, 2048d), WorldRepresentationKind.Far);
            TerrainRepresentationData a = builder.Build(left, level, generator);
            TerrainRepresentationData b = builder.Build(right, level, generator);
            int resolution = level.TerrainResolution;
            for (int z = 0; z < resolution; z++)
                Assert.That(a.Vertices[z * resolution + resolution - 1].Y, Is.EqualTo(b.Vertices[z * resolution].Y));
        }

        [Test]
        public void SharedHeightSource_FullRepresentationMatchesDetailedTerrain()
        {
            TerrainGenerationConfig config = TerrainGenerationConfig.CreateSandboxDefault();
            BiomeDefinition biome = BiomeDefinition.CreateTemperateGrassland();
            ZoneDNA dna = new ZoneDNA(new ZoneId(7), 12345, BiomeId.TemperateGrassland,
                TerrainProfileId.RollingLowPoly, TerrainGeneratorV4.GeneratorVersion, new AssetCatalogVersion(3));
            ZoneFeaturePlan features = new ZoneFeaturePlannerV1(WorldGenerationLimits.Sandbox).Plan(dna, config);
            HeightFieldGeneratorV2 source = new HeightFieldGeneratorV2(dna, config, biome, features);
            HierarchicalWorldGenerator hierarchy = new HierarchicalWorldGenerator(source, biome, config, dna.Seed, TerrainGeneratorV4.GeneratorVersion.Value);
            HeightSample detailed = source.Sample(184f, -321f);
            HierarchicalHeightSample represented = hierarchy.Sample(184d, -321d,
                TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro);
            Assert.That(represented.Height, Is.EqualTo(detailed.Height));
        }

        [Test]
        public void Build_ExclusionBounds_RemovesDetailedWorldTrianglesOnly()
        {
            HierarchicalWorldGenerator generator = new HierarchicalWorldGenerator(99, 4);
            WorldRepresentationLevel level = WorldRepresentationProfile.CreateDefault()[WorldRepresentationKind.Far];
            WorldCell cell = new WorldCell(new WorldCellCoordinate(0, 0, 0), new WorldBounds(0d, 0d, 2048d), WorldRepresentationKind.Far);
            TerrainRepresentationBuilder builder = new TerrainRepresentationBuilder();
            TerrainRepresentationData complete = builder.Build(cell, level, generator);
            TerrainRepresentationData clipped = builder.Build(cell, level, generator, new WorldBounds(0d, 0d, 1024d));
            Assert.That(clipped.Triangles.Length, Is.GreaterThan(0));
            Assert.That(clipped.Triangles.Length, Is.LessThan(complete.Triangles.Length));
        }

        [Test]
        public void AssessDetailed_OversizedEagerTerrain_IsRejectedInFavorOfHierarchy()
        {
            TerrainGenerationConfig oversized = new TerrainGenerationConfig(8000f, 8000f, 481, 900f, 460000, 24, 24, TerrainShadingMode.Smooth);
            TerrainScaleAssessment assessment = TerrainScalabilityPolicy.AssessDetailed(oversized);
            Assert.That(assessment.IsSupported, Is.False);
            Assert.That(assessment.Reason, Does.Contain("hierarchical"));
            Assert.That(TerrainScalabilityPolicy.AssessDetailed(TerrainGenerationConfig.CreateLargeSandboxDefault()).IsSupported, Is.True);
        }

        [Test]
        public void ScalableHighlands_UsesVersionedSharedMacroWorldWithinDetailedLimits()
        {
            TerrainGenerationConfig config = new TerrainGenerationConfig(5000f, 5000f, 401, 420f, 320000, 20, 20, TerrainShadingMode.Smooth);
            BiomeDefinition biome = BiomeDefinition.CreateExpandedTemperateGrassland();
            LargeScaleTerrainProfile profile = LargeScaleTerrainProfile.CreateScalableHighlands();
            ZoneDNA dna = new ZoneDNA(new ZoneId(10), 48151623, BiomeId.TemperateGrassland,
                TerrainProfileId.RollingLowPoly, TerrainGeneratorV5.GeneratorVersion, new AssetCatalogVersion(3));
            ZoneGenerationResult result = new ZoneGeneratorV5(config, biome, profile, WorldGenerationLimits.ScalableHighlands).Generate(dna);
            float minimum = float.MaxValue, maximum = float.MinValue;
            float[] heights = result.Terrain.HeightField.CopyHeights();
            for (int i = 0; i < heights.Length; i++) { minimum = System.Math.Min(minimum, heights[i]); maximum = System.Math.Max(maximum, heights[i]); }
            Assert.That(TerrainScalabilityPolicy.AssessDetailed(config).IsSupported, Is.True);
            Assert.That(maximum - minimum, Is.GreaterThan(120f));
            Assert.That(result.Decorations.Count, Is.GreaterThan(1500));

            HeightFieldGeneratorV2 source = new HeightFieldGeneratorV2(dna, config, biome, result.Features, profile);
            HierarchicalWorldGenerator hierarchy = new HierarchicalWorldGenerator(source, biome, config, dna.Seed, TerrainGeneratorV5.GeneratorVersion.Value);
            float detailed = result.Terrain.HeightField.SampleHeight(0f, 0f);
            float represented = hierarchy.Sample(0d, 0d,
                TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro).Height;
            Assert.That(represented, Is.EqualTo(detailed).Within(0.001f));
        }

        [Test]
        public void GeologicalLandforms_DepressionAndHillRespectPhysicalEnvelopes()
        {
            ZoneDNA dna = new ZoneDNA(new ZoneId(10), 48151623, BiomeId.TemperateGrassland,
                TerrainProfileId.RollingLowPoly, TerrainGeneratorV6.GeneratorVersion, new AssetCatalogVersion(3));
            LandformDNA depression = new LandformDNA(new WorldElementId(1), dna, WorldElementKind.Depression, 42,
                new WorldElementBounds(0f, 0f, 100f), -75f, 1f);
            LandformDNA hill = new LandformDNA(new WorldElementId(2), dna, WorldElementKind.Hill, 84,
                new WorldElementBounds(0f, 0f, 100f), 200f, 1f);
            double depressionDepth = -GeologicalLandformModel.Evaluate(depression, 0d, 0d);
            double hillHeight = GeologicalLandformModel.Evaluate(hill, 0d, 0d);
            Assert.That(depressionDepth, Is.LessThanOrEqualTo(100d * GeologicalLandformModel.MaximumDepressionDepthRatio));
            Assert.That(hillHeight, Is.LessThanOrEqualTo(100d * System.Math.Tan(GeologicalLandformModel.MaximumHillSlopeDegrees * System.Math.PI / 180d) * 0.48d));
            Assert.That(GeologicalLandformModel.Evaluate(depression, 100d, 0d), Is.Zero);
            Assert.That(GeologicalLandformModel.Evaluate(hill, 100d, 0d), Is.Zero);
        }

    }
}
