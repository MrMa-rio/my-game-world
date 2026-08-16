using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class EnvironmentalSurfaceResolver
    {
        private readonly ZoneGenerationResult _zone;
        public EnvironmentalSurfaceResolver(ZoneGenerationResult zone) { _zone = zone; }

        public EnvironmentalSurfaceKind Resolve(Vector3 position, EnvironmentalBiomeKind biome)
        {
            if (_zone == null) return DefaultForBiome(biome);
            for (int index = 0; index < _zone.Features.Liquids.Count; index++)
                if (_zone.Features.Liquids[index].Bounds.Contains(position.x, position.z)) return EnvironmentalSurfaceKind.Water;
            float path = _zone.Terrain.HeightField.SamplePathMask(position.x, position.z);
            float slope = 1f - _zone.Terrain.HeightField.SampleNormal(position.x, position.z).Y;
            if (slope > 0.2f) return EnvironmentalSurfaceKind.Rock;
            if (path > 0.38f) return EnvironmentalSurfaceKind.DrySoil;
            return DefaultForBiome(biome);
        }

        private static EnvironmentalSurfaceKind DefaultForBiome(EnvironmentalBiomeKind biome)
        {
            switch (biome)
            {
                case EnvironmentalBiomeKind.Desert: return EnvironmentalSurfaceKind.Sand;
                case EnvironmentalBiomeKind.Snow: return EnvironmentalSurfaceKind.Snow;
                default: return EnvironmentalSurfaceKind.Grass;
            }
        }
    }
}
