using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public sealed class DecorationGeneratorV1
    {
        private const uint DecorationScope = 0x4445434F;
        private const uint HabitatScope = 0x48414254;
        private readonly WorldGenerationLimits _limits;
        private readonly bool _useHabitatDistribution;

        public DecorationGeneratorV1(WorldGenerationLimits limits, bool useHabitatDistribution = false)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _useHabitatDistribution = useHabitatDistribution;
        }

        public IReadOnlyList<DecorationPlacement> Generate(
            ZoneDNA dna,
            TerrainGenerationResult terrain,
            BiomeDefinition biome)
        {
            if (dna == null)
            {
                throw new ArgumentNullException(nameof(dna));
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (biome == null)
            {
                throw new ArgumentNullException(nameof(biome));
            }

            TerrainGenerationConfig config = terrain.Config;
            TerrainHeightField field = terrain.HeightField;
            int targetCount = Math.Min(Math.Max(1, _limits.MaxDecorations - 4), Math.Max(1, (int)Math.Round(config.Width * config.Depth * biome.DecorationDensity)));
            int cellsPerAxis = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(targetCount)));
            float cellWidth = config.Width / cellsPerAxis;
            float cellDepth = config.Depth / cellsPerAxis;
            List<DecorationPlacement> placements = new List<DecorationPlacement>(targetCount + 4);
            long habitatSeed = SeedDerivation.Derive(dna.Seed, HabitatScope, dna.ZoneId.Value);

            for (int cellZ = 0; cellZ < cellsPerAxis; cellZ++)
            {
                for (int cellX = 0; cellX < cellsPerAxis; cellX++)
                {
                    long localId = (cellZ * cellsPerAxis) + cellX + 1L;
                    long candidateSeed = SeedDerivation.Derive(dna.Seed, DecorationScope, localId);
                    DeterministicRandom random = new DeterministicRandom(candidateSeed);
                    float x = (-config.Width * 0.5f) + ((cellX + Lerp(0.18f, 0.82f, random)) * cellWidth);
                    float z = (-config.Depth * 0.5f) + ((cellZ + Lerp(0.18f, 0.82f, random)) * cellDepth);
                    float pathMask = field.SamplePathMask(x, z);
                    WorldVector3 normal = field.SampleNormal(x, z);
                    float slope = 1f - normal.Y;
                    float height = field.SampleHeight(x, z);
                    float habitat = (float)((DeterministicNoise2D.FractalBrownianMotion(habitatSeed, x / 135d, z / 135d, 3) + 1d) * 0.5d);

                    if (pathMask > 0.34f || !HasMinimumDistance(placements, x, z, biome.MinimumDecorationDistance))
                    {
                        continue;
                    }

                    if (_useHabitatDistribution && random.NextUnitDouble() > 0.58d + (habitat * 0.34d))
                    {
                        continue;
                    }

                    DecorationKind kind = ResolveKind(random, slope, biome.RockSlopeThreshold, _useHabitatDistribution ? habitat : -1f);
                    float yaw = (float)(random.NextUnitDouble() * 360d);
                    float scale = ResolveScale(kind, random);
                    placements.Add(new DecorationPlacement(
                        new WorldElementId(10000L + localId), dna, candidateSeed,
                        kind,
                        WorldVisualAssetIds.ForDecoration(kind),
                        new WorldVector3(x, height, z),
                        yaw,
                        scale,
                        Lerp(0.82f, 1.18f, random),
                        Lerp(0.82f, 1.18f, random),
                        Lerp(0.82f, 1.18f, random)));
                }
            }

            AddScaleMarkers(placements, field, config, dna, cellsPerAxis * cellsPerAxis + 1L);
            return placements;
        }

        private static DecorationKind ResolveKind(
            DeterministicRandom random,
            float slope,
            float rockSlopeThreshold,
            float habitat = -1f)
        {
            if (slope >= rockSlopeThreshold * 0.82f)
            {
                return DecorationKind.Rock;
            }

            int roll = random.NextInt(100);
            int treeThreshold = habitat < 0f ? 52 : 38 + (int)Math.Round(habitat * 30f);
            if (roll < treeThreshold)
            {
                return DecorationKind.Tree;
            }

            int bushThreshold = habitat < 0f ? 78 : treeThreshold + 24;
            return roll < bushThreshold ? DecorationKind.Bush : DecorationKind.Rock;
        }

        private static float ResolveScale(DecorationKind kind, DeterministicRandom random)
        {
            float unit = (float)random.NextUnitDouble();
            switch (kind)
            {
                case DecorationKind.Tree:
                    return 0.82f + (unit * 0.55f);
                case DecorationKind.Rock:
                    return 0.65f + (unit * 0.75f);
                case DecorationKind.Bush:
                    return 0.65f + (unit * 0.45f);
                default:
                    return 1f;
            }
        }

        private static bool HasMinimumDistance(
            IReadOnlyList<DecorationPlacement> placements,
            float x,
            float z,
            float minimumDistance)
        {
            float minimumSquared = minimumDistance * minimumDistance;
            for (int index = 0; index < placements.Count; index++)
            {
                DecorationPlacement placement = placements[index];
                if (placement.Kind == DecorationKind.ScaleMarker)
                {
                    continue;
                }

                float deltaX = placement.Position.X - x;
                float deltaZ = placement.Position.Z - z;
                if ((deltaX * deltaX) + (deltaZ * deltaZ) < minimumSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddScaleMarkers(
            ICollection<DecorationPlacement> placements,
            TerrainHeightField field,
            TerrainGenerationConfig config,
            ZoneDNA dna,
            long firstId)
        {
            float offsetX = config.Width * 0.32f;
            float offsetZ = config.Depth * 0.32f;
            AddScaleMarker(placements, field, dna, firstId, -offsetX, -offsetZ);
            AddScaleMarker(placements, field, dna, firstId + 1, offsetX, -offsetZ);
            AddScaleMarker(placements, field, dna, firstId + 2, -offsetX, offsetZ);
            AddScaleMarker(placements, field, dna, firstId + 3, offsetX, offsetZ);
        }

        private static void AddScaleMarker(
            ICollection<DecorationPlacement> placements,
            TerrainHeightField field,
            ZoneDNA dna,
            long id,
            float x,
            float z)
        {
            placements.Add(new DecorationPlacement(
                new WorldElementId(10000L + id), dna, SeedDerivation.Derive(dna.Seed, DecorationScope, id),
                DecorationKind.ScaleMarker,
                WorldVisualAssetIds.DevelopmentScaleMarker,
                new WorldVector3(x, field.SampleHeight(x, z), z),
                0f,
                1f));
        }

        private static float Lerp(float minimum, float maximum, DeterministicRandom random)
        {
            return minimum + ((maximum - minimum) * (float)random.NextUnitDouble());
        }
    }
}
