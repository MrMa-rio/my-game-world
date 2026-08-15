using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainSurfaceDNA : WorldElementDNA
    {
        public TerrainSurfaceDNA(WorldElementId id, ZoneDNA zone, WorldElementBounds bounds)
            : base(id, zone.ZoneId, WorldElementKind.TerrainSurface, zone.Seed, zone.GeneratorVersion, zone.AssetCatalogVersion, bounds) { }
    }

    public sealed class LandformDNA : WorldElementDNA
    {
        public LandformDNA(WorldElementId id, ZoneDNA zone, WorldElementKind kind, long seed,
            WorldElementBounds bounds, float amplitude, float falloffPower)
            : base(id, zone.ZoneId, kind, seed, zone.GeneratorVersion, zone.AssetCatalogVersion, bounds)
        {
            if (kind != WorldElementKind.Hill && kind != WorldElementKind.Depression && kind != WorldElementKind.Plateau)
                throw new ArgumentOutOfRangeException(nameof(kind));
            Amplitude = amplitude;
            FalloffPower = falloffPower;
        }

        public float Amplitude { get; }
        public float FalloffPower { get; }
    }

    public sealed class PathDNA : WorldElementDNA
    {
        public PathDNA(WorldElementId id, ZoneDNA zone, long seed, WorldElementBounds bounds,
            WorldVector3 start, WorldVector3 control, WorldVector3 end, float width, float flattenStrength)
            : base(id, zone.ZoneId, WorldElementKind.Path, seed, zone.GeneratorVersion, zone.AssetCatalogVersion, bounds)
        {
            if (width <= 0f) throw new ArgumentOutOfRangeException(nameof(width));
            Start = start; Control = control; End = end; Width = width; FlattenStrength = flattenStrength;
        }

        public WorldVector3 Start { get; }
        public WorldVector3 Control { get; }
        public WorldVector3 End { get; }
        public float Width { get; }
        public float FlattenStrength { get; }
    }
}
