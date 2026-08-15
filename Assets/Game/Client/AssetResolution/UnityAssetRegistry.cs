using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using UnityEngine;

namespace MyGameWorld.Client.AssetResolution
{
    public sealed class UnityAssetRegistry : IAssetRegistry<UnityEngine.Object>
    {
        private readonly Dictionary<AssetId, UnityEngine.Object> _assets;

        public UnityAssetRegistry(UnityAssetCatalog catalog)
            : this(
                catalog != null ? catalog.Version : throw new ArgumentNullException(nameof(catalog)),
                catalog.Bindings)
        {
        }

        public UnityAssetRegistry(
            AssetCatalogVersion version,
            IReadOnlyList<UnityAssetBinding> bindings)
        {
            if (version.Value == 0)
            {
                throw new ArgumentException("A valid catalog version is required.", nameof(version));
            }

            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            Version = version;
            _assets = new Dictionary<AssetId, UnityEngine.Object>(bindings.Count);

            for (int index = 0; index < bindings.Count; index++)
            {
                UnityAssetBinding binding = bindings[index];
                if (binding == null)
                {
                    throw new ArgumentException("Asset bindings cannot contain null entries.", nameof(bindings));
                }

                if (binding.RawAssetId == 0)
                {
                    throw new ArgumentException("Asset binding ID zero is reserved.", nameof(bindings));
                }

                if (binding.Asset == null)
                {
                    throw new ArgumentException($"Asset binding {binding.AssetId} has no Unity object.", nameof(bindings));
                }

                if (!_assets.TryAdd(binding.AssetId, binding.Asset))
                {
                    throw new ArgumentException($"Asset ID {binding.AssetId} is duplicated.", nameof(bindings));
                }
            }
        }

        public AssetCatalogVersion Version { get; }

        public bool TryResolve(AssetId assetId, out UnityEngine.Object asset)
        {
            return _assets.TryGetValue(assetId, out asset);
        }
    }
}
