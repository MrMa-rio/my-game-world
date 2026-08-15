using System;
using MyGameWorld.Shared.Core;
using UnityEngine;

namespace MyGameWorld.Client.AssetResolution
{
    [Serializable]
    public sealed class UnityAssetBinding
    {
        [SerializeField]
        private uint _assetId;

        [SerializeField]
        private UnityEngine.Object _asset;

        public UnityAssetBinding(uint assetId, UnityEngine.Object asset)
        {
            _assetId = assetId;
            _asset = asset;
        }

        public AssetId AssetId => new AssetId(_assetId);

        public uint RawAssetId => _assetId;

        public UnityEngine.Object Asset => _asset;
    }
}
