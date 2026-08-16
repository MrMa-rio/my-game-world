using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public sealed class DecorationGeneratorV3
    {
        private readonly DecorationGeneratorV1 _implementation;

        public DecorationGeneratorV3(WorldGenerationLimits limits)
        {
            _implementation = new DecorationGeneratorV1(limits ?? throw new ArgumentNullException(nameof(limits)),
                useHabitatDistribution: true, useGroundFlora: true, useNaturalClusters: true);
        }

        public IReadOnlyList<DecorationPlacement> Generate(ZoneDNA dna, TerrainGenerationResult terrain, BiomeDefinition biome)
        {
            return _implementation.Generate(dna, terrain, biome);
        }
    }
}
