using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class CharacterAppearanceGeneratorTests
    {
        [Test]
        public void Generate_SameSeed_ProducesSameAppearance()
        {
            List<CharacterPartDefinition> parts = CreateParts(); CharacterAppearanceGenerator generator = new CharacterAppearanceGenerator();
            CharacterAppearanceDNA first = generator.Generate(42, new AssetCatalogVersion(1), AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame, parts);
            CharacterAppearanceDNA second = generator.Generate(42, new AssetCatalogVersion(1), AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame, parts);
            Assert.That(second.Parts, Is.EqualTo(first.Parts)); Assert.That(second.SkinTone, Is.EqualTo(first.SkinTone));
            Assert.That(second.HairTone, Is.EqualTo(first.HairTone)); Assert.That(second.ClothingPalette, Is.EqualTo(first.ClothingPalette));
        }

        [Test]
        public void Generate_IncompatiblePart_IsNeverSelected()
        {
            List<CharacterPartDefinition> parts = CreateParts();
            parts.Add(Part(99, CharacterPartSlot.Hair, AssetCategory.Hair, AssetTrait.LargeFrame));
            CharacterAppearanceDNA appearance = new CharacterAppearanceGenerator().Generate(10, new AssetCatalogVersion(1), AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame, parts);
            Assert.That(appearance.Parts, Has.None.Matches<CharacterPartSelection>(part => part.AssetId == new AssetId(99)));
        }

        [Test]
        public void Constructor_CategoryDoesNotMatchSlot_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => Part(1, CharacterPartSlot.Hair, AssetCategory.Equipment, AssetTrait.None));
        }

        private static List<CharacterPartDefinition> CreateParts() => new List<CharacterPartDefinition>
        {
            Part(1, CharacterPartSlot.Body, AssetCategory.CharacterBody, AssetTrait.HumanoidSkeleton),
            Part(2, CharacterPartSlot.Head, AssetCategory.Head, AssetTrait.HumanoidSkeleton),
            Part(3, CharacterPartSlot.Hair, AssetCategory.Hair, AssetTrait.HumanoidSkeleton),
            Part(4, CharacterPartSlot.Hair, AssetCategory.Hair, AssetTrait.HumanoidSkeleton),
            Part(5, CharacterPartSlot.UpperClothing, AssetCategory.Equipment, AssetTrait.HumanoidSkeleton)
        };

        private static CharacterPartDefinition Part(uint id, CharacterPartSlot slot, AssetCategory category, AssetTrait required) =>
            new CharacterPartDefinition(slot, new AssetDescriptor(new AssetId(id), category, AssetTrait.None,
                new AssetCompatibility(required, AssetTrait.None)));
    }
}
