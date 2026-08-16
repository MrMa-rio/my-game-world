using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using UnityEngine;

namespace MyGameWorld.Client.AssetResolution
{
    [CreateAssetMenu(fileName = "AssetCatalog", menuName = "My Game World/Asset Catalog")]
    public sealed class UnityAssetCatalog : ScriptableObject
    {
        [SerializeField, Min(1)]
        private ushort _version = 1;

        [SerializeField]
        private List<UnityAssetBinding> _bindings = new List<UnityAssetBinding>();

        public AssetCatalogVersion Version => new AssetCatalogVersion(_version);

        public IReadOnlyList<UnityAssetBinding> Bindings => _bindings;

        public void Configure(ushort version, IReadOnlyList<UnityAssetBinding> bindings)
        {
            if (version == 0) throw new System.ArgumentOutOfRangeException(nameof(version));
            if (bindings == null) throw new System.ArgumentNullException(nameof(bindings));
            _version = version; _bindings = new List<UnityAssetBinding>(bindings);
        }
    }
}
