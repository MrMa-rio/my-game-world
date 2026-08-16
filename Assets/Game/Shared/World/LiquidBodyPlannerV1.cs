using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public sealed class LiquidBodyPlannerV1
    {
        private const uint LiquidScope = 0x4C495155;

        public IReadOnlyList<LiquidBodyDNA> Plan(ZoneDNA dna, TerrainGenerationResult terrain, ZoneFeaturePlan features)
        {
            if (dna == null || terrain == null || features == null) throw new ArgumentNullException();
            LandformDNA basin = null;
            for (int index = 0; index < features.Landforms.Count; index++)
            {
                LandformDNA candidate = features.Landforms[index];
                if (candidate.ElementKind == WorldElementKind.Depression && (basin == null || candidate.Bounds.Radius > basin.Bounds.Radius))
                    basin = candidate;
            }
            if (basin == null) return new LiquidBodyDNA[0];

            long seed = SeedDerivation.Derive(dna.Seed, LiquidScope, basin.ElementId.Value);
            DeterministicRandom random = new DeterministicRandom(seed);
            float radiusX = basin.Bounds.Radius * (0.56f + (float)random.NextUnitDouble() * 0.08f);
            float radiusZ = basin.Bounds.Radius * (0.48f + (float)random.NextUnitDouble() * 0.1f);
            float depth = Math.Max(0.8f, Math.Abs(basin.Amplitude) * 0.34f);
            float volume = (float)Math.PI * radiusX * radiusZ * depth * 0.55f;
            float bottom = terrain.HeightField.SampleHeight(basin.Bounds.CenterX, basin.Bounds.CenterZ);
            float surface = bottom + depth * 0.68f;
            return new[]
            {
                new LiquidBodyDNA(new WorldElementId(9000), dna, seed, LiquidSubstance.Water, volume, surface,
                    radiusX, radiusZ, 0f, new WorldElementBounds(basin.Bounds.CenterX, basin.Bounds.CenterZ, Math.Max(radiusX, radiusZ)))
            };
        }
    }
}
