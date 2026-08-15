using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    [Serializable]
    public sealed class ZoneDNA : IEquatable<ZoneDNA>
    {
        public ZoneDNA(
            ZoneId zoneId,
            long seed,
            BiomeId biomeId,
            TerrainProfileId terrainProfileId,
            GeneratorVersion generatorVersion,
            AssetCatalogVersion assetCatalogVersion)
        {
            if (!Enum.IsDefined(typeof(BiomeId), biomeId))
            {
                throw new ArgumentOutOfRangeException(nameof(biomeId));
            }

            if (!Enum.IsDefined(typeof(TerrainProfileId), terrainProfileId))
            {
                throw new ArgumentOutOfRangeException(nameof(terrainProfileId));
            }

            ZoneId = zoneId;
            Seed = seed;
            BiomeId = biomeId;
            TerrainProfileId = terrainProfileId;
            GeneratorVersion = generatorVersion;
            AssetCatalogVersion = assetCatalogVersion;
        }

        public ZoneId ZoneId { get; }

        public long Seed { get; }

        public BiomeId BiomeId { get; }

        public TerrainProfileId TerrainProfileId { get; }

        public GeneratorVersion GeneratorVersion { get; }

        public AssetCatalogVersion AssetCatalogVersion { get; }

        public bool Equals(ZoneDNA other)
        {
            return other != null
                && ZoneId == other.ZoneId
                && Seed == other.Seed
                && BiomeId == other.BiomeId
                && TerrainProfileId == other.TerrainProfileId
                && GeneratorVersion == other.GeneratorVersion
                && AssetCatalogVersion == other.AssetCatalogVersion;
        }

        public override bool Equals(object obj) => Equals(obj as ZoneDNA);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ZoneId.GetHashCode();
                hash = (hash * 397) ^ Seed.GetHashCode();
                hash = (hash * 397) ^ (int)BiomeId;
                hash = (hash * 397) ^ (int)TerrainProfileId;
                hash = (hash * 397) ^ GeneratorVersion.GetHashCode();
                return (hash * 397) ^ AssetCatalogVersion.GetHashCode();
            }
        }
    }
}
