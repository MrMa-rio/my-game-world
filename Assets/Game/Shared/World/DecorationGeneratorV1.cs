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
        private readonly bool _useGroundFlora;
        private readonly bool _useNaturalClusters;

        public DecorationGeneratorV1(WorldGenerationLimits limits, bool useHabitatDistribution = false, bool useGroundFlora = false,
            bool useNaturalClusters = false)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _useHabitatDistribution = useHabitatDistribution;
            _useGroundFlora = useGroundFlora;
            _useNaturalClusters = useNaturalClusters;
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

                    DecorationKind kind = ResolveKind(random, slope, biome.RockSlopeThreshold,
                        _useHabitatDistribution ? habitat : -1f, _useGroundFlora, _useNaturalClusters);
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
            float habitat = -1f,
            bool useGroundFlora = false,
            bool useNaturalClusters = false)
        {
            if (slope >= rockSlopeThreshold * 0.82f)
            {
                return DecorationKind.Rock;
            }

            int roll = random.NextInt(100);
            if (useGroundFlora)
            {
                if (useNaturalClusters)
                {
                    if (roll < 5) return DecorationKind.Flower;
                    if (roll < 9) return DecorationKind.FlowerCluster;
                    if (roll < 12) return habitat >= 0.45f ? DecorationKind.Mushroom : DecorationKind.Bush;
                    if (roll < 14) return habitat >= 0.62f ? DecorationKind.MushroomCluster : DecorationKind.Rock;
                    if (roll < 20) return DecorationKind.TreeCluster;
                    if (roll < 24) return DecorationKind.BushCluster;
                    if (roll < 28) return DecorationKind.RockCluster;
                    int clusteredTreeThreshold = 54 + (int)Math.Round(Math.Max(0f, habitat) * 18f);
                    if (roll < clusteredTreeThreshold) return DecorationKind.Tree;
                    return roll < 84 ? DecorationKind.Bush : DecorationKind.Rock;
                }
                if (roll < 7) return DecorationKind.Flower;
                if (roll < 12) return DecorationKind.FlowerCluster;
                if (roll < 16) return habitat >= 0.45f ? DecorationKind.Mushroom : DecorationKind.Bush;
                if (roll < 19) return habitat >= 0.62f ? DecorationKind.MushroomCluster : DecorationKind.Rock;
                int treeThresholdV2 = 50 + (int)Math.Round(Math.Max(0f, habitat) * 20f);
                if (roll < treeThresholdV2) return DecorationKind.Tree;
                return roll < 84 ? DecorationKind.Bush : DecorationKind.Rock;
            }
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
                case DecorationKind.Flower:
                    return 0.52f + (unit * 0.34f);
                case DecorationKind.FlowerCluster:
                    return 0.62f + (unit * 0.38f);
                case DecorationKind.Mushroom:
                    return 0.48f + (unit * 0.42f);
                case DecorationKind.MushroomCluster:
                    return 0.58f + (unit * 0.42f);
                case DecorationKind.TreeCluster:
                    return 0.82f + (unit * 0.32f);
                case DecorationKind.RockCluster:
                    return 0.72f + (unit * 0.46f);
                case DecorationKind.BushCluster:
                    return 0.68f + (unit * 0.38f);
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
