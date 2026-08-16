using System;
using MyGameWorld.Shared.Procedural;

namespace MyGameWorld.Shared.World
{
    public sealed class ZoneGeneratorV5
    {
        private readonly TerrainGenerationConfig _config; private readonly BiomeDefinition _biome;
        private readonly WorldGenerationLimits _limits; private readonly LargeScaleTerrainProfile _largeScale;
        public ZoneGeneratorV5(TerrainGenerationConfig config, BiomeDefinition biome, LargeScaleTerrainProfile largeScale, WorldGenerationLimits limits = null)
        { _config = config ?? throw new ArgumentNullException(nameof(config)); _biome = biome ?? throw new ArgumentNullException(nameof(biome)); _largeScale = largeScale ?? throw new ArgumentNullException(nameof(largeScale)); _limits = limits ?? WorldGenerationLimits.Sandbox; }
        public ZoneGenerationResult Generate(ZoneDNA dna)
        {
            ZoneFeaturePlan features = new ZoneFeaturePlannerV1(_limits).Plan(dna, _config);
            GenerationContext context = new GenerationContext(dna.Seed, dna.GeneratorVersion, dna.AssetCatalogVersion);
            TerrainGenerationResult terrain = new TerrainGeneratorV5(_config, _biome, features, _largeScale).Generate(dna, context);
            var liquids = new LiquidBodyPlannerV1().Plan(dna, terrain, features);
            ZoneFeaturePlan completed = new ZoneFeaturePlan(features.Terrain, features.Landforms, features.Paths, liquids);
            return new ZoneGenerationResult(dna, terrain, new DecorationGeneratorV3(_limits).Generate(dna, terrain, _biome), completed);
        }
    }
}
