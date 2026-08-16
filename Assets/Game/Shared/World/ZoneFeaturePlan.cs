using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public sealed class ZoneFeaturePlan
    {
        private readonly LandformDNA[] _landforms;
        private readonly PathDNA[] _paths;
        private readonly LiquidBodyDNA[] _liquids;

        public ZoneFeaturePlan(TerrainSurfaceDNA terrain, IReadOnlyList<LandformDNA> landforms, IReadOnlyList<PathDNA> paths,
            IReadOnlyList<LiquidBodyDNA> liquids = null)
        {
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _landforms = Copy(landforms);
            _paths = Copy(paths);
            _liquids = liquids == null ? new LiquidBodyDNA[0] : Copy(liquids);
        }

        public TerrainSurfaceDNA Terrain { get; }
        public IReadOnlyList<LandformDNA> Landforms => _landforms;
        public IReadOnlyList<PathDNA> Paths => _paths;
        public IReadOnlyList<LiquidBodyDNA> Liquids => _liquids;
        public int ElementCount => 1 + _landforms.Length + _paths.Length + _liquids.Length;

        public IReadOnlyList<WorldElementDNA> ResolveAt(float x, float z)
        {
            List<WorldElementDNA> matches = new List<WorldElementDNA> { Terrain };
            for (int i = 0; i < _landforms.Length; i++) if (_landforms[i].Bounds.Contains(x, z)) matches.Add(_landforms[i]);
            for (int i = 0; i < _paths.Length; i++) if (TerrainFeatureMath.PathInfluence(_paths[i], x, z) > 0f) matches.Add(_paths[i]);
            for (int i = 0; i < _liquids.Length; i++) if (_liquids[i].Bounds.Contains(x, z)) matches.Add(_liquids[i]);
            return matches;
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            T[] result = new T[source.Count]; for (int i = 0; i < source.Count; i++) result[i] = source[i]; return result;
        }
    }
}
