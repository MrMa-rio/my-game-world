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
    }
}
