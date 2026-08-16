using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public readonly struct CharacterPartSelection : IEquatable<CharacterPartSelection>
    {
        public CharacterPartSelection(CharacterPartSlot slot, AssetId assetId) { Slot = slot; AssetId = assetId; }
        public CharacterPartSlot Slot { get; }
        public AssetId AssetId { get; }
        public bool Equals(CharacterPartSelection other) => Slot == other.Slot && AssetId == other.AssetId;
        public override bool Equals(object obj) => obj is CharacterPartSelection other && Equals(other);
        public override int GetHashCode() => ((int)Slot * 397) ^ AssetId.GetHashCode();
    }

    public sealed class CharacterAppearanceDNA
    {
        private readonly CharacterPartSelection[] _parts;
        public CharacterAppearanceDNA(long seed, GeneratorVersion generatorVersion, AssetCatalogVersion catalogVersion,
            AssetTrait traits, IReadOnlyList<CharacterPartSelection> parts, byte skinTone, byte hairTone, byte clothingPalette)
        {
            if (parts == null) throw new ArgumentNullException(nameof(parts));
            Seed = seed; GeneratorVersion = generatorVersion; AssetCatalogVersion = catalogVersion; Traits = traits;
            SkinTone = skinTone; HairTone = hairTone; ClothingPalette = clothingPalette;
            _parts = new CharacterPartSelection[parts.Count];
            HashSet<CharacterPartSlot> slots = new HashSet<CharacterPartSlot>();
            for (int i = 0; i < parts.Count; i++)
            {
                if (!slots.Add(parts[i].Slot)) throw new ArgumentException("Appearance contains a duplicate slot.", nameof(parts));
                _parts[i] = parts[i];
            }
        }
        public long Seed { get; }
        public GeneratorVersion GeneratorVersion { get; }
        public AssetCatalogVersion AssetCatalogVersion { get; }
        public AssetTrait Traits { get; }
        public byte SkinTone { get; }
        public byte HairTone { get; }
        public byte ClothingPalette { get; }
        public IReadOnlyList<CharacterPartSelection> Parts => _parts;
    }

    public sealed class CharacterAppearanceGenerator
    {
        public static readonly GeneratorVersion Version = new GeneratorVersion(1);

        public CharacterAppearanceDNA Generate(long seed, AssetCatalogVersion catalogVersion, AssetTrait traits,
            IReadOnlyList<CharacterPartDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            DeterministicRandom random = new DeterministicRandom(seed);
            List<CharacterPartSelection> selected = new List<CharacterPartSelection>();
            foreach (CharacterPartSlot slot in Enum.GetValues(typeof(CharacterPartSlot)))
            {
                SelectSlot(slot, traits, definitions, random, selected);
            }
            return new CharacterAppearanceDNA(seed, Version, catalogVersion, traits, selected,
                (byte)random.NextInt(16), (byte)random.NextInt(24), (byte)random.NextInt(32));
        }

        private static void SelectSlot(CharacterPartSlot slot, AssetTrait traits, IReadOnlyList<CharacterPartDefinition> definitions,
            DeterministicRandom random, List<CharacterPartSelection> selected)
        {
            ulong total = 0; bool optional = true;
            for (int i = 0; i < definitions.Count; i++) if (definitions[i].Slot == slot && definitions[i].IsCompatibleWith(traits))
            { total = checked(total + definitions[i].SelectionWeight); optional &= definitions[i].Optional; }
            if (total == 0) return;
            if (optional && random.NextInt(4) == 0) return;
            ulong roll = random.NextUInt64(total), cumulative = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                CharacterPartDefinition part = definitions[i];
                if (part.Slot != slot || !part.IsCompatibleWith(traits)) continue;
                cumulative += part.SelectionWeight;
                if (roll < cumulative) { selected.Add(new CharacterPartSelection(slot, part.Asset.AssetId)); return; }
            }
        }
    }
}
