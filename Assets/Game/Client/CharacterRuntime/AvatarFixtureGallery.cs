using System;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Shared.Procedural;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    [DisallowMultipleComponent]
    public sealed class AvatarFixtureGallery : MonoBehaviour
    {
        [SerializeField] private UnityAssetCatalog _assetCatalog;
        [SerializeField] private AvatarPartCatalog _partCatalog;
        [SerializeField] private Transform _avatarParent;
        [SerializeField] private long _seed = 100;
        [SerializeField] private bool _masculine = true;
        private AvatarCreationManager _manager; private RuntimeAvatar _avatar;

        public void Configure(UnityAssetCatalog assetCatalog, AvatarPartCatalog partCatalog, Transform avatarParent)
        { _assetCatalog = assetCatalog; _partCatalog = partCatalog; _avatarParent = avatarParent; }

        private void Awake()
        {
            if (_assetCatalog == null || _partCatalog == null) { enabled = false; return; }
            _manager = gameObject.AddComponent<AvatarCreationManager>();
            _manager.Initialize(new UnityAssetRegistry(_assetCatalog), _partCatalog.CreateDefinitions()); Spawn();
        }

        private void Spawn()
        {
            if (_manager == null) return; if (_avatar != null) _manager.Release(_avatar);
            AssetTrait family = _masculine ? AssetTrait.MasculineFrame : AssetTrait.FeminineFrame;
            _avatar = _manager.CreateImmediately(_seed, AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame | family, _avatarParent);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 290, 230), GUI.skin.box);
            GUILayout.Label("Procedural Avatar Gallery"); GUILayout.Label($"Seed: {_seed}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Seed")) { _seed--; Spawn(); }
            if (GUILayout.Button("Regenerate")) Spawn();
            if (GUILayout.Button("Seed >")) { _seed++; Spawn(); }
            GUILayout.EndHorizontal();
            GUILayout.Label(_masculine ? "Body family: Masculine" : "Body family: Feminine");
            if (GUILayout.Button("Change body family")) { _masculine = !_masculine; Spawn(); }
            AvatarRuntimeMetrics metrics = _manager != null ? _manager.Metrics : default;
            GUILayout.Label($"Parts: {metrics.ActiveParts}  Cache: {metrics.CacheHits}/{metrics.CacheMisses}");
            GUILayout.Label("Temporary CC0 validation fixture"); GUILayout.Label("Final visual identity will be original.");
            GUILayout.EndArea();
        }
    }
}
