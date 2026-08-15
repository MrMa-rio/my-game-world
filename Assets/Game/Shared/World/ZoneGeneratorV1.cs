using System;
using MyGameWorld.Shared.Procedural;

namespace MyGameWorld.Shared.World
{
    public sealed class ZoneGeneratorV1
    {
        private readonly BiomeDefinition _biome;
        private readonly TerrainGenerationConfig _config;
        private readonly WorldGenerationLimits _limits;

        public ZoneGeneratorV1(TerrainGenerationConfig config, BiomeDefinition biome, WorldGenerationLimits limits = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _biome = biome ?? throw new ArgumentNullException(nameof(biome));
            _limits = limits ?? WorldGenerationLimits.LegacySandbox;
        }

        public ZoneGenerationResult Generate(ZoneDNA dna)
        {
            if (dna == null)
            {
                throw new ArgumentNullException(nameof(dna));
            }

            ZoneFeaturePlan features = new ZoneFeaturePlannerV1(_limits).Plan(dna, _config);
            TerrainGeneratorV1 terrainGenerator = new TerrainGeneratorV1(_config, _biome, features);
            DecorationGeneratorV1 decorationGenerator = new DecorationGeneratorV1(_limits);
            GenerationContext context = new GenerationContext(
                dna.Seed,
                dna.GeneratorVersion,
                dna.AssetCatalogVersion);
            TerrainGenerationResult terrain = terrainGenerator.Generate(dna, context);
            return new ZoneGenerationResult(
                dna,
                terrain,
                decorationGenerator.Generate(dna, terrain, _biome),
                features);
        }
    }
}
