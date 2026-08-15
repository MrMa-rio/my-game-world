using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.EntityModel;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct GenerationContext
    {
        public GenerationContext(
            long seed,
            GeneratorVersion generatorVersion,
            AssetCatalogVersion assetCatalogVersion)
        {
            Seed = seed;
            GeneratorVersion = generatorVersion;
            AssetCatalogVersion = assetCatalogVersion;
        }

        public long Seed { get; }

        public GeneratorVersion GeneratorVersion { get; }

        public AssetCatalogVersion AssetCatalogVersion { get; }

        public DeterministicRandom CreateRandom() => new DeterministicRandom(Seed);

        public static GenerationContext From(EntityDNA dna)
        {
            return new GenerationContext(dna.Seed, dna.GeneratorVersion, dna.AssetCatalogVersion);
        }
    }
}
