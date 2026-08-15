using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public sealed class AssetCatalog
    {
        private readonly AssetCatalogEntry[] _entries;

        public AssetCatalog(AssetCatalogVersion version, IReadOnlyList<AssetCatalogEntry> entries)
        {
            if (version.Value == 0)
            {
                throw new ArgumentException("A valid catalog version is required.", nameof(version));
            }

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Count == 0)
            {
                throw new ArgumentException("The asset catalog cannot be empty.", nameof(entries));
            }

            Version = version;
            _entries = new AssetCatalogEntry[entries.Count];
            HashSet<AssetId> knownIds = new HashSet<AssetId>();
            ulong totalWeight = 0;

            for (int index = 0; index < entries.Count; index++)
            {
                AssetCatalogEntry entry = entries[index];
                if (entry.AssetId.Value == 0 || entry.SelectionWeight == 0)
                {
                    throw new ArgumentException("Catalog entries must contain a valid ID and positive weight.", nameof(entries));
                }

                if (!knownIds.Add(entry.AssetId))
                {
                    throw new ArgumentException($"Asset ID {entry.AssetId} is duplicated.", nameof(entries));
                }

                totalWeight = checked(totalWeight + entry.SelectionWeight);
                _entries[index] = entry;
            }

            TotalSelectionWeight = totalWeight;
        }

        public AssetCatalogVersion Version { get; }

        public int Count => _entries.Length;

        public ulong TotalSelectionWeight { get; }

        public AssetCatalogEntry this[int index] => _entries[index];

        public bool Contains(AssetId assetId)
        {
            for (int index = 0; index < _entries.Length; index++)
            {
                if (_entries[index].AssetId == assetId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
