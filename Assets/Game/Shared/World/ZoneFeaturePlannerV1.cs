using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public sealed class ZoneFeaturePlannerV1
    {
        private const uint LandformScope = 0x4C414E44;
        private const uint PathScope = 0x50415448;
        private readonly WorldGenerationLimits _limits;

        public ZoneFeaturePlannerV1(WorldGenerationLimits limits)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        }

        public ZoneFeaturePlan Plan(ZoneDNA zone, TerrainGenerationConfig config)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (config == null) throw new ArgumentNullException(nameof(config));
            float zoneRadius = (float)Math.Sqrt(config.Width * config.Width + config.Depth * config.Depth) * 0.5f;
            TerrainSurfaceDNA terrain = new TerrainSurfaceDNA(new WorldElementId(1), zone, new WorldElementBounds(0f, 0f, zoneRadius));
            List<LandformDNA> landforms = CreateLandforms(zone, config);
            List<PathDNA> paths = CreatePaths(zone, landforms);
            return new ZoneFeaturePlan(terrain, landforms, paths);
        }

        private List<LandformDNA> CreateLandforms(ZoneDNA zone, TerrainGenerationConfig config)
        {
            float linearScale = (float)Math.Sqrt((config.Width * config.Depth) / 10000f);
            int count = Math.Min(_limits.MaxLandforms, Math.Max(6, (int)Math.Round(6f + ((linearScale - 1f) * 2.1f))));
            List<LandformDNA> result = new List<LandformDNA>(count);
            float halfWidth = Math.Max(1f, config.Width * 0.5f - _limits.BoundaryPadding);
            float halfDepth = Math.Max(1f, config.Depth * 0.5f - _limits.BoundaryPadding);
            for (int i = 0; i < count; i++)
            {
                long id = 1001L + i;
                long seed = SeedDerivation.Derive(zone.Seed, LandformScope, id);
                DeterministicRandom random = new DeterministicRandom(seed);
                WorldElementKind kind = ResolveLandformKind(random.NextInt(100));
                float radiusScale = (float)Math.Sqrt(Math.Max(1f, linearScale));
                float radius = Math.Min(Math.Min(_limits.MaxLandformRadius, Math.Min(halfWidth, halfDepth)), Lerp(13f, 25f, random) * radiusScale);
                float x = Lerp(-halfWidth + radius, halfWidth - radius, random);
                float z = Lerp(-halfDepth + radius, halfDepth - radius, random);
                float amplitudeScale = 1f + ((float)(Math.Log(Math.Max(1f, linearScale), 2d)) * 0.25f);
                float magnitude = Math.Min(_limits.MaxLandformAmplitude, Lerp(2.5f, 6.5f, random) * amplitudeScale);
                float amplitude = kind == WorldElementKind.Depression ? -magnitude : kind == WorldElementKind.Plateau ? Lerp(-0.8f, 1.8f, random) : magnitude;
                result.Add(new LandformDNA(new WorldElementId(id), zone, kind, seed,
                    new WorldElementBounds(x, z, radius), amplitude, Lerp(0.75f, 1.4f, random)));
            }
            return result;
        }

        private List<PathDNA> CreatePaths(ZoneDNA zone, IReadOnlyList<LandformDNA> landforms)
        {
            int count = Math.Min(_limits.MaxPaths, Math.Max(2, landforms.Count / 4));
            List<PathDNA> result = new List<PathDNA>(count);
            for (int i = 0; i < count; i++)
            {
                long id = 2001L + i; long seed = SeedDerivation.Derive(zone.Seed, PathScope, id);
                DeterministicRandom random = new DeterministicRandom(seed);
                LandformDNA first = landforms[i]; LandformDNA second = landforms[landforms.Count - 1 - i];
                float sx = first.Bounds.CenterX; float sz = first.Bounds.CenterZ;
                float ex = second.Bounds.CenterX; float ez = second.Bounds.CenterZ;
                float dx = ex - sx; float dz = ez - sz; float length = (float)Math.Sqrt(dx * dx + dz * dz);
                float bend = Lerp(-0.18f, 0.18f, random) * length;
                float cx = (sx + ex) * 0.5f + (length > 0f ? -dz / length * bend : 0f);
                float cz = (sz + ez) * 0.5f + (length > 0f ? dx / length * bend : 0f);
                float radius = length * 0.5f + 5f;
                result.Add(new PathDNA(new WorldElementId(id), zone, seed,
                    new WorldElementBounds((sx + ex) * 0.5f, (sz + ez) * 0.5f, radius),
                    new WorldVector3(sx, 0f, sz), new WorldVector3(cx, 0f, cz), new WorldVector3(ex, 0f, ez),
                    Lerp(1.8f, 2.8f, random) * (float)Math.Sqrt(Math.Max(1f, landforms.Count / 6f)), Lerp(0.58f, 0.78f, random)));
            }
            return result;
        }

        private static WorldElementKind ResolveLandformKind(int roll) => roll < 55 ? WorldElementKind.Hill : roll < 78 ? WorldElementKind.Depression : WorldElementKind.Plateau;
        private static float Lerp(float min, float max, DeterministicRandom random) => min + (max - min) * (float)random.NextUnitDouble();
    }
}
