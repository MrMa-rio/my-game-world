using System;
using MyGameWorld.Shared.Procedural;

namespace MyGameWorld.Shared.World
{
    public sealed class ZoneGeneratorV3
    {
        private readonly TerrainGenerationConfig _config;
        private readonly BiomeDefinition _biome;
        private readonly WorldGenerationLimits _limits;

        public ZoneGeneratorV3(TerrainGenerationConfig config, BiomeDefinition biome, WorldGenerationLimits limits = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _biome = biome ?? throw new ArgumentNullException(nameof(biome));
            _limits = limits ?? WorldGenerationLimits.Sandbox;
        }

        public ZoneGenerationResult Generate(ZoneDNA dna)
        {
            if (dna == null) throw new ArgumentNullException(nameof(dna));
            ZoneFeaturePlan features = new ZoneFeaturePlannerV1(_limits).Plan(dna, _config);
            GenerationContext context = new GenerationContext(dna.Seed, dna.GeneratorVersion, dna.AssetCatalogVersion);
            TerrainGenerationResult terrain = new TerrainGeneratorV3(_config, _biome, features).Generate(dna, context);
            return new ZoneGenerationResult(dna, terrain, new DecorationGeneratorV2(_limits).Generate(dna, terrain, _biome), features);
        }
    }
}
