using System;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct CharacterPartDefinition : IEquatable<CharacterPartDefinition>
    {
        public CharacterPartDefinition(CharacterPartSlot slot, AssetDescriptor asset, uint selectionWeight = 1, bool optional = false)
        {
            if (!Enum.IsDefined(typeof(CharacterPartSlot), slot)) throw new ArgumentOutOfRangeException(nameof(slot));
            if (selectionWeight == 0) throw new ArgumentOutOfRangeException(nameof(selectionWeight));
            ValidateCategory(slot, asset.Category);
            Slot = slot; Asset = asset; SelectionWeight = selectionWeight; Optional = optional;
        }

        public CharacterPartSlot Slot { get; }
        public AssetDescriptor Asset { get; }
        public uint SelectionWeight { get; }
        public bool Optional { get; }

        public bool IsCompatibleWith(AssetTrait characterTraits) => Asset.Compatibility.Accepts(characterTraits);

        public bool Equals(CharacterPartDefinition other) => Slot == other.Slot && Asset.Equals(other.Asset)
            && SelectionWeight == other.SelectionWeight && Optional == other.Optional;
        public override bool Equals(object obj) => obj is CharacterPartDefinition other && Equals(other);
        public override int GetHashCode() => (((int)Slot * 397) ^ Asset.GetHashCode()) * 397 ^ (int)SelectionWeight;

        private static void ValidateCategory(CharacterPartSlot slot, AssetCategory category)
        {
            bool valid = slot switch
            {
                CharacterPartSlot.Body => category == AssetCategory.CharacterBody,
                CharacterPartSlot.Head => category == AssetCategory.Head || category == AssetCategory.CharacterBody,
                CharacterPartSlot.Hair => category == AssetCategory.Hair,
                CharacterPartSlot.UpperClothing or CharacterPartSlot.LowerClothing or CharacterPartSlot.Feet => category == AssetCategory.Equipment,
                CharacterPartSlot.Hands => category == AssetCategory.CharacterBody || category == AssetCategory.Equipment,
                CharacterPartSlot.Accessory => category == AssetCategory.Accessory || category == AssetCategory.Equipment,
                _ => category == AssetCategory.CharacterBody || category == AssetCategory.Head || category == AssetCategory.Accessory
            };
            if (!valid) throw new ArgumentException($"Asset category {category} is invalid for character slot {slot}.", nameof(category));
        }
    }
}
