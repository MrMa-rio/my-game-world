using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    public readonly struct AvatarCreationRequest
    {
        public AvatarCreationRequest(long seed, AssetTrait traits, Transform parent, Action<RuntimeAvatar> completed = null,
            AvatarEnvironmentContext environment = default)
        { Seed = seed; Traits = traits; Parent = parent; Completed = completed; Environment = environment; }
        public long Seed { get; }
        public AssetTrait Traits { get; }
        public Transform Parent { get; }
        public Action<RuntimeAvatar> Completed { get; }
        public AvatarEnvironmentContext Environment { get; }
    }

    public readonly struct AvatarRuntimeMetrics
    {
        public AvatarRuntimeMetrics(int active, int queued, int pooled, int parts, int cacheHits, int cacheMisses)
        { ActiveAvatars = active; QueuedRequests = queued; PooledRoots = pooled; ActiveParts = parts; CacheHits = cacheHits; CacheMisses = cacheMisses; }
        public int ActiveAvatars { get; }
        public int QueuedRequests { get; }
        public int PooledRoots { get; }
        public int ActiveParts { get; }
        public int CacheHits { get; }
        public int CacheMisses { get; }
    }

    [DisallowMultipleComponent]
    public sealed class AvatarCreationManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _maxAvatarsPerFrame = 2;
        private readonly Queue<AvatarCreationRequest> _queue = new Queue<AvatarCreationRequest>();
        private readonly List<RuntimeAvatar> _active = new List<RuntimeAvatar>();
        private readonly Stack<RuntimeAvatar> _pool = new Stack<RuntimeAvatar>();
        private readonly Dictionary<AppearanceKey, CharacterAppearanceDNA> _appearanceCache = new Dictionary<AppearanceKey, CharacterAppearanceDNA>();
        private CharacterAppearanceGenerator _generator;
        private IAssetRegistry<UnityEngine.Object> _registry;
        private IReadOnlyList<CharacterPartDefinition> _definitions;
        private int _cacheHits, _cacheMisses;

        public bool IsInitialized => _registry != null;
        public AvatarRuntimeMetrics Metrics
        {
            get { int parts = 0; for (int i = 0; i < _active.Count; i++) parts += _active[i].PartCount;
                return new AvatarRuntimeMetrics(_active.Count, _queue.Count, _pool.Count, parts, _cacheHits, _cacheMisses); }
        }

        public void Initialize(IAssetRegistry<UnityEngine.Object> registry, IReadOnlyList<CharacterPartDefinition> definitions)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            if (_definitions.Count == 0) throw new ArgumentException("Avatar part definitions cannot be empty.", nameof(definitions));
            _generator = new CharacterAppearanceGenerator();
        }

        public void Request(AvatarCreationRequest request) { EnsureInitialized(); _queue.Enqueue(request); }

        public RuntimeAvatar CreateImmediately(long seed, AssetTrait traits, Transform parent = null,
            AvatarEnvironmentContext environment = default)
        {
            AvatarStyleRecipe style = AvatarEnvironmentalStyleResolver.Resolve(seed, environment);
            EnsureInitialized(); CharacterAppearanceDNA appearance = ResolveAppearance(style.AppearanceSeed, traits);
            RuntimeAvatar avatar = _pool.Count > 0 ? _pool.Pop() : new GameObject("Runtime Avatar").AddComponent<RuntimeAvatar>();
            avatar.transform.SetParent(parent, false); avatar.gameObject.SetActive(true); avatar.Initialize(appearance, style);
            for (int i = 0; i < appearance.Parts.Count; i++) MaterializePart(avatar, appearance.Parts[i]);
            _active.Add(avatar); return avatar;
        }

        public void Release(RuntimeAvatar avatar)
        {
            if (avatar == null || !_active.Remove(avatar)) return;
            avatar.ResetAvatar(); avatar.gameObject.SetActive(false); avatar.transform.SetParent(transform, false); _pool.Push(avatar);
        }

        private void Update()
        {
            int count = Mathf.Min(_maxAvatarsPerFrame, _queue.Count);
            for (int i = 0; i < count; i++) { AvatarCreationRequest request = _queue.Dequeue(); RuntimeAvatar avatar = CreateImmediately(request.Seed, request.Traits, request.Parent, request.Environment); request.Completed?.Invoke(avatar); }
        }

        private CharacterAppearanceDNA ResolveAppearance(long seed, AssetTrait traits)
        {
            AppearanceKey key = new AppearanceKey(seed, traits, _registry.Version);
            if (_appearanceCache.TryGetValue(key, out CharacterAppearanceDNA cached)) { _cacheHits++; return cached; }
            _cacheMisses++; CharacterAppearanceDNA appearance = _generator.Generate(seed, _registry.Version, traits, _definitions);
            _appearanceCache.Add(key, appearance); return appearance;
        }

        private void MaterializePart(RuntimeAvatar avatar, CharacterPartSelection selection)
        {
            if (!_registry.TryResolve(selection.AssetId, out UnityEngine.Object asset))
                throw new InvalidOperationException($"Avatar asset {selection.AssetId} for slot {selection.Slot} is absent from catalog {_registry.Version}.");
            if (!(asset is GameObject prefab)) throw new InvalidOperationException($"Avatar asset {selection.AssetId} must resolve to a GameObject prefab.");
            GameObject part = Instantiate(prefab, avatar.ResolveAnchor(selection.Slot), false); part.name = $"{selection.Slot} [{selection.AssetId.Value}]";
            ApplyPalette(part, avatar.Appearance, avatar.Style, selection.Slot); avatar.AddPart(part);
        }

        private static void ApplyPalette(GameObject part, CharacterAppearanceDNA appearance, AvatarStyleRecipe style, CharacterPartSlot slot)
        {
            float index = slot == CharacterPartSlot.Hair ? appearance.HairTone :
                slot == CharacterPartSlot.UpperClothing || slot == CharacterPartSlot.LowerClothing || slot == CharacterPartSlot.Feet
                    ? appearance.ClothingPalette : appearance.SkinTone;
            MaterialPropertyBlock block = new MaterialPropertyBlock(); block.SetFloat("_PaletteIndex", index);
            block.SetVector("_AvatarStyleTint", style.ColorTint);
            block.SetFloat("_AvatarAngularity", style.Angularity);
            float tintStrength = slot == CharacterPartSlot.UpperClothing || slot == CharacterPartSlot.LowerClothing
                || slot == CharacterPartSlot.Feet || slot == CharacterPartSlot.Accessory ? 0.28f
                : slot == CharacterPartSlot.Hair ? 0.14f : 0.06f;
            Color contextualTint = Color.Lerp(Color.white, style.ColorTint, tintStrength);
            block.SetColor("_BaseColor", contextualTint);
            block.SetColor("_Color", contextualTint);
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].SetPropertyBlock(block);
        }

        private void EnsureInitialized() { if (!IsInitialized) throw new InvalidOperationException("AvatarCreationManager.Initialize must be called first."); }

        private readonly struct AppearanceKey : IEquatable<AppearanceKey>
        {
            public AppearanceKey(long seed, AssetTrait traits, AssetCatalogVersion version) { Seed = seed; Traits = traits; Version = version; }
            private long Seed { get; } private AssetTrait Traits { get; } private AssetCatalogVersion Version { get; }
            public bool Equals(AppearanceKey other) => Seed == other.Seed && Traits == other.Traits && Version == other.Version;
            public override bool Equals(object obj) => obj is AppearanceKey other && Equals(other);
            public override int GetHashCode() => ((Seed.GetHashCode() * 397) ^ Traits.GetHashCode()) * 397 ^ Version.GetHashCode();
        }
    }
}
