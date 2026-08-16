using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public enum WorldRepresentationKind : byte
    {
        Unloaded,
        Metadata,
        Horizon,
        Distant,
        Far,
        Medium,
        Near,
        Simulation
    }

    [Flags]
    public enum TerrainFrequencyBand : byte
    {
        None = 0,
        Macro = 1,
        Meso = 2,
        Micro = 4
    }

    public readonly struct GlobalPosition : IEquatable<GlobalPosition>
    {
        public GlobalPosition(double x, double y, double z) { X = x; Y = y; Z = z; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public bool Equals(GlobalPosition other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is GlobalPosition other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() ^ (Y.GetHashCode() * 397) ^ (Z.GetHashCode() * 7919);
        public GlobalPosition Add(double x, double y, double z) => new GlobalPosition(X + x, Y + y, Z + z);
    }

    public readonly struct WorldCellCoordinate : IEquatable<WorldCellCoordinate>
    {
        public WorldCellCoordinate(long x, long z, byte depth) { X = x; Z = z; Depth = depth; }
        public long X { get; }
        public long Z { get; }
        public byte Depth { get; }
        public bool Equals(WorldCellCoordinate other) => X == other.X && Z == other.Z && Depth == other.Depth;
        public override bool Equals(object obj) => obj is WorldCellCoordinate other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() ^ (Z.GetHashCode() * 397) ^ Depth;
        public override string ToString() => $"({X},{Z})@{Depth}";
    }

    public readonly struct WorldBounds
    {
        public WorldBounds(double minimumX, double minimumZ, double size)
        {
            if (size <= 0d) throw new ArgumentOutOfRangeException(nameof(size));
            MinimumX = minimumX; MinimumZ = minimumZ; Size = size;
        }
        public double MinimumX { get; }
        public double MinimumZ { get; }
        public double Size { get; }
        public double CenterX => MinimumX + Size * 0.5d;
        public double CenterZ => MinimumZ + Size * 0.5d;
        public bool Contains(double x, double z) => x >= MinimumX && x <= MinimumX + Size && z >= MinimumZ && z <= MinimumZ + Size;
        public double DistanceTo(double x, double z)
        {
            double dx = Math.Max(Math.Max(MinimumX - x, 0d), x - (MinimumX + Size));
            double dz = Math.Max(Math.Max(MinimumZ - z, 0d), z - (MinimumZ + Size));
            return Math.Sqrt(dx * dx + dz * dz);
        }
    }

    public sealed class WorldRepresentationLevel
    {
        public WorldRepresentationLevel(WorldRepresentationKind kind, int terrainResolution,
            TerrainFrequencyBand frequencies, float assetDensity, bool shadows, bool physics,
            bool simulation, float materialComplexity, float environmentalVfxLevel)
        {
            if (terrainResolution < 2) throw new ArgumentOutOfRangeException(nameof(terrainResolution));
            Kind = kind; TerrainResolution = terrainResolution; Frequencies = frequencies;
            AssetDensity = Clamp01(assetDensity); Shadows = shadows; Physics = physics;
            Simulation = simulation; MaterialComplexity = Clamp01(materialComplexity);
            EnvironmentalVfxLevel = Clamp01(environmentalVfxLevel);
        }
        public WorldRepresentationKind Kind { get; }
        public int TerrainResolution { get; }
        public TerrainFrequencyBand Frequencies { get; }
        public float AssetDensity { get; }
        public bool Shadows { get; }
        public bool Physics { get; }
        public bool Simulation { get; }
        public float MaterialComplexity { get; }
        public float EnvironmentalVfxLevel { get; }
        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }

    public sealed class WorldRepresentationProfile
    {
        private readonly Dictionary<WorldRepresentationKind, WorldRepresentationLevel> _levels;
        private readonly double[] _maximumDistances;

        public WorldRepresentationProfile(IReadOnlyList<WorldRepresentationLevel> levels,
            double simulationDistance, double nearDistance, double mediumDistance, double farDistance,
            double distantDistance, double horizonDistance, double hysteresisFraction = 0.08d)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            if (!(simulationDistance < nearDistance && nearDistance < mediumDistance && mediumDistance < farDistance &&
                farDistance < distantDistance && distantDistance < horizonDistance))
                throw new ArgumentException("Representation distances must be strictly increasing.");
            _levels = new Dictionary<WorldRepresentationKind, WorldRepresentationLevel>();
            for (int i = 0; i < levels.Count; i++) _levels[levels[i].Kind] = levels[i];
            _maximumDistances = new[] { simulationDistance, nearDistance, mediumDistance, farDistance, distantDistance, horizonDistance };
            HysteresisFraction = Math.Max(0d, Math.Min(0.4d, hysteresisFraction));
        }

        public double HysteresisFraction { get; }
        public double HorizonDistance => _maximumDistances[5];
        public WorldRepresentationLevel this[WorldRepresentationKind kind] => _levels[kind];

        public WorldRepresentationKind Resolve(double distance, WorldRepresentationKind previous = WorldRepresentationKind.Unloaded)
        {
            WorldRepresentationKind raw = distance <= _maximumDistances[0] ? WorldRepresentationKind.Simulation :
                distance <= _maximumDistances[1] ? WorldRepresentationKind.Near :
                distance <= _maximumDistances[2] ? WorldRepresentationKind.Medium :
                distance <= _maximumDistances[3] ? WorldRepresentationKind.Far :
                distance <= _maximumDistances[4] ? WorldRepresentationKind.Distant :
                distance <= _maximumDistances[5] ? WorldRepresentationKind.Horizon : WorldRepresentationKind.Metadata;
            int previousBand = DistanceBand(previous);
            int rawBand = DistanceBand(raw);
            if (previousBand < 0 || rawBand == previousBand) return raw;
            int boundary = Math.Min(previousBand, rawBand);
            if (boundary >= _maximumDistances.Length) return raw;
            double margin = _maximumDistances[boundary] * HysteresisFraction;
            if (rawBand > previousBand && distance < _maximumDistances[boundary] + margin) return previous;
            if (rawBand < previousBand && distance > _maximumDistances[boundary] - margin) return previous;
            return raw;
        }

        public static WorldRepresentationProfile CreateDefault() => new WorldRepresentationProfile(
            new[] {
                new WorldRepresentationLevel(WorldRepresentationKind.Simulation, 129, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro, 1f, true, true, true, 1f, 1f),
                new WorldRepresentationLevel(WorldRepresentationKind.Near, 129, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro, 1f, true, false, false, 1f, 0.8f),
                new WorldRepresentationLevel(WorldRepresentationKind.Medium, 65, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro, 0.45f, true, false, false, 0.7f, 0.35f),
                new WorldRepresentationLevel(WorldRepresentationKind.Far, 33, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso, 0.12f, false, false, false, 0.45f, 0.1f),
                new WorldRepresentationLevel(WorldRepresentationKind.Distant, 17, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso, 0.02f, false, false, false, 0.2f, 0f),
                new WorldRepresentationLevel(WorldRepresentationKind.Horizon, 9, TerrainFrequencyBand.Macro, 0f, false, false, false, 0.08f, 0f),
            }, 200d, 2000d, 5000d, 12000d, 25000d, 50000d);

        private static int DistanceBand(WorldRepresentationKind kind)
        {
            switch (kind) {
                case WorldRepresentationKind.Simulation: return 0; case WorldRepresentationKind.Near: return 1;
                case WorldRepresentationKind.Medium: return 2; case WorldRepresentationKind.Far: return 3;
                case WorldRepresentationKind.Distant: return 4; case WorldRepresentationKind.Horizon: return 5;
                case WorldRepresentationKind.Metadata: return 6; default: return -1;
            }
        }
    }

    public sealed class WorldCell
    {
        public WorldCell(WorldCellCoordinate coordinate, WorldBounds bounds, WorldRepresentationKind representation)
        { Coordinate = coordinate; Bounds = bounds; Representation = representation; RequestedRepresentation = representation; }
        public WorldCellCoordinate Coordinate { get; }
        public WorldBounds Bounds { get; }
        public WorldRepresentationKind Representation { get; internal set; }
        public WorldRepresentationKind RequestedRepresentation { get; internal set; }
        public long LastTouchedFrame { get; internal set; }
        public object CachedData { get; internal set; }
    }

    public sealed class WorldStreamingBudget
    {
        public WorldStreamingBudget(double maxCpuMillisecondsPerFrame = 2d, int maxMeshCommitsPerFrame = 2,
            int maxRepresentationChangesPerFrame = 4, int maxGenerationJobs = 2)
        {
            MaxCpuMillisecondsPerFrame = Math.Max(0.1d, maxCpuMillisecondsPerFrame);
            MaxMeshCommitsPerFrame = Math.Max(1, maxMeshCommitsPerFrame);
            MaxRepresentationChangesPerFrame = Math.Max(1, maxRepresentationChangesPerFrame);
            MaxGenerationJobs = Math.Max(1, maxGenerationJobs);
        }
        public double MaxCpuMillisecondsPerFrame { get; }
        public int MaxMeshCommitsPerFrame { get; }
        public int MaxRepresentationChangesPerFrame { get; }
        public int MaxGenerationJobs { get; }
    }

    public sealed class WorldSpatialHierarchy
    {
        private readonly double _minimumCellSize;
        private readonly byte _maximumDepth;
        public WorldSpatialHierarchy(double minimumCellSize = 256d, byte maximumDepth = 8)
        { if (minimumCellSize <= 0d) throw new ArgumentOutOfRangeException(nameof(minimumCellSize)); _minimumCellSize = minimumCellSize; _maximumDepth = maximumDepth; }

        public IReadOnlyList<WorldCell> Select(GlobalPosition viewer, WorldRepresentationProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            double rootSize = _minimumCellSize * (1L << _maximumDepth);
            long rootX = (long)Math.Floor(viewer.X / rootSize);
            long rootZ = (long)Math.Floor(viewer.Z / rootSize);
            List<WorldCell> result = new List<WorldCell>();
            // A 3x3 root neighborhood guarantees coverage while the viewer crosses an aligned root boundary.
            for (long z = rootZ - 1; z <= rootZ + 1; z++) for (long x = rootX - 1; x <= rootX + 1; x++)
                Visit(x, z, 0, new WorldBounds(x * rootSize, z * rootSize, rootSize), viewer, profile, result);
            return result;
        }

        private void Visit(long x, long z, byte depth, WorldBounds bounds, GlobalPosition viewer,
            WorldRepresentationProfile profile, ICollection<WorldCell> result)
        {
            double distance = bounds.DistanceTo(viewer.X, viewer.Z);
            if (distance > profile.HorizonDistance) return;
            WorldRepresentationKind level = profile.Resolve(distance);
            // Screen-error proxy: closer cells subdivide until their span is appropriate to their representation.
            double desiredSpan = level == WorldRepresentationKind.Simulation ? 256d : level == WorldRepresentationKind.Near ? 512d :
                level == WorldRepresentationKind.Medium ? 1024d : level == WorldRepresentationKind.Far ? 2048d :
                level == WorldRepresentationKind.Distant ? 4096d : 8192d;
            if (depth < _maximumDepth && bounds.Size > desiredSpan)
            {
                double half = bounds.Size * 0.5d;
                byte childDepth = (byte)(depth + 1);
                Visit(x * 2, z * 2, childDepth, new WorldBounds(bounds.MinimumX, bounds.MinimumZ, half), viewer, profile, result);
                Visit(x * 2 + 1, z * 2, childDepth, new WorldBounds(bounds.MinimumX + half, bounds.MinimumZ, half), viewer, profile, result);
                Visit(x * 2, z * 2 + 1, childDepth, new WorldBounds(bounds.MinimumX, bounds.MinimumZ + half, half), viewer, profile, result);
                Visit(x * 2 + 1, z * 2 + 1, childDepth, new WorldBounds(bounds.MinimumX + half, bounds.MinimumZ + half, half), viewer, profile, result);
                return;
            }
            result.Add(new WorldCell(new WorldCellCoordinate(x, z, depth), bounds, level));
        }
    }
}
