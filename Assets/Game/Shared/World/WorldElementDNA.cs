using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public enum WorldElementKind
    {
        TerrainSurface = 1,
        Hill = 2,
        Depression = 3,
        Plateau = 4,
        Path = 5,
        Tree = 6,
        Rock = 7,
        Bush = 8,
        ScaleMarker = 9
    }

    public readonly struct WorldElementId : IEquatable<WorldElementId>
    {
        public WorldElementId(long value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public long Value { get; }
        public bool Equals(WorldElementId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is WorldElementId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct WorldElementBounds
    {
        public WorldElementBounds(float centerX, float centerZ, float radius)
        {
            if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            CenterX = centerX;
            CenterZ = centerZ;
            Radius = radius;
        }

        public float CenterX { get; }
        public float CenterZ { get; }
        public float Radius { get; }

        public bool Contains(float x, float z)
        {
            float dx = x - CenterX;
            float dz = z - CenterZ;
            return (dx * dx) + (dz * dz) <= Radius * Radius;
        }
    }

    public abstract class WorldElementDNA
    {
        protected WorldElementDNA(
            WorldElementId elementId,
            ZoneId zoneId,
            WorldElementKind kind,
            long seed,
            GeneratorVersion generatorVersion,
            AssetCatalogVersion assetCatalogVersion,
            WorldElementBounds bounds)
        {
            if (!Enum.IsDefined(typeof(WorldElementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            ElementId = elementId;
            ZoneId = zoneId;
            ElementKind = kind;
            Seed = seed;
            GeneratorVersion = generatorVersion;
            AssetCatalogVersion = assetCatalogVersion;
            Bounds = bounds;
        }

        public WorldElementId ElementId { get; }
        public ZoneId ZoneId { get; }
        public WorldElementKind ElementKind { get; }
        public long Seed { get; }
        public GeneratorVersion GeneratorVersion { get; }
        public AssetCatalogVersion AssetCatalogVersion { get; }
        public WorldElementBounds Bounds { get; }
    }
}
