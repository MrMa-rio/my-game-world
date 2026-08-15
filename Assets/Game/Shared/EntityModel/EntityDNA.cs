using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.EntityModel
{
    [Serializable]
    public sealed class EntityDNA : IEquatable<EntityDNA>
    {
        public EntityDNA(
            EntityId entityId,
            ArchetypeId archetypeId,
            long seed,
            GeneratorVersion generatorVersion,
            AssetCatalogVersion assetCatalogVersion)
        {
            EntityId = entityId;
            ArchetypeId = archetypeId;
            Seed = seed;
            GeneratorVersion = generatorVersion;
            AssetCatalogVersion = assetCatalogVersion;
        }

        public EntityId EntityId { get; }

        public ArchetypeId ArchetypeId { get; }

        public long Seed { get; }

        public GeneratorVersion GeneratorVersion { get; }

        public AssetCatalogVersion AssetCatalogVersion { get; }

        public bool Equals(EntityDNA other)
        {
            return other != null
                && EntityId == other.EntityId
                && ArchetypeId == other.ArchetypeId
                && Seed == other.Seed
                && GeneratorVersion == other.GeneratorVersion
                && AssetCatalogVersion == other.AssetCatalogVersion;
        }

        public override bool Equals(object obj) => Equals(obj as EntityDNA);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = EntityId.GetHashCode();
                hash = (hash * 397) ^ ArchetypeId.GetHashCode();
                hash = (hash * 397) ^ Seed.GetHashCode();
                hash = (hash * 397) ^ GeneratorVersion.GetHashCode();
                return (hash * 397) ^ AssetCatalogVersion.GetHashCode();
            }
        }
    }
}
