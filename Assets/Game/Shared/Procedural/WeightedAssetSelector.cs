using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public static class WeightedAssetSelector
    {
        public static AssetId Select(AssetCatalog catalog, DeterministicRandom random)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            ulong selection = random.NextUInt64(catalog.TotalSelectionWeight);
            ulong cumulativeWeight = 0;

            for (int index = 0; index < catalog.Count; index++)
            {
                AssetCatalogEntry entry = catalog[index];
                cumulativeWeight += entry.SelectionWeight;
                if (selection < cumulativeWeight)
                {
                    return entry.AssetId;
                }
            }

            throw new InvalidOperationException("Catalog weight invariant was violated.");
        }
    }
}
