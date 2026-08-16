using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    [Serializable]
    public sealed class AvatarPartCatalogEntry
    {
        [SerializeField] private uint _assetId;
        [SerializeField] private CharacterPartSlot _slot;
        [SerializeField] private AssetCategory _category;
        [SerializeField] private long _traitsBits;
        [SerializeField] private long _requiredTraitsBits;
        [SerializeField, Min(1)] private uint _weight = 1;
        [SerializeField] private bool _optional;

        public AvatarPartCatalogEntry(uint assetId, CharacterPartSlot slot, AssetCategory category, AssetTrait traits,
            AssetTrait requiredTraits, uint weight = 1, bool optional = false)
        { _assetId = assetId; _slot = slot; _category = category; _traitsBits = unchecked((long)traits); _requiredTraitsBits = unchecked((long)requiredTraits); _weight = weight; _optional = optional; }

        public CharacterPartDefinition ToDefinition() => new CharacterPartDefinition(_slot,
            new AssetDescriptor(new AssetId(_assetId), _category, unchecked((AssetTrait)_traitsBits),
                new AssetCompatibility(unchecked((AssetTrait)_requiredTraitsBits), AssetTrait.None)), _weight, _optional);
    }

    [CreateAssetMenu(fileName = "AvatarPartCatalog", menuName = "My Game World/Avatar Part Catalog")]
    public sealed class AvatarPartCatalog : ScriptableObject
    {
        [SerializeField] private List<AvatarPartCatalogEntry> _entries = new List<AvatarPartCatalogEntry>();
        public int Count => _entries.Count;
        public CharacterPartDefinition[] CreateDefinitions()
        { CharacterPartDefinition[] result = new CharacterPartDefinition[_entries.Count]; for (int i = 0; i < result.Length; i++) result[i] = _entries[i].ToDefinition(); return result; }
        public void Configure(IReadOnlyList<AvatarPartCatalogEntry> entries)
        { if (entries == null) throw new ArgumentNullException(nameof(entries)); _entries = new List<AvatarPartCatalogEntry>(entries); }
    }
}
