using System;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class AssetCompatibilityTests
    {
        [Test]
        public void Constructor_SameRequiredAndExcludedTrait_Throws()
        {
            Assert.Throws<ArgumentException>(() => new AssetCompatibility(
                AssetTrait.HumanoidSkeleton,
                AssetTrait.HumanoidSkeleton));
        }

        [Test]
        public void AreCompatible_MutualRequirementsSatisfied_ReturnsTrue()
        {
            AssetDescriptor body = CreateDescriptor(
                1,
                AssetCategory.CharacterBody,
                AssetTrait.HumanoidSkeleton | AssetTrait.HairSocket,
                required: AssetTrait.None);
            AssetDescriptor hair = CreateDescriptor(
                2,
                AssetCategory.Hair,
                AssetTrait.HumanoidSkeleton,
                required: AssetTrait.HairSocket);

            Assert.That(AssetCompatibilityEvaluator.AreCompatible(body, hair), Is.True);
        }

        [Test]
        public void AreCompatible_RequiredTraitMissing_ReturnsFalse()
        {
            AssetDescriptor body = CreateDescriptor(
                1,
                AssetCategory.CharacterBody,
                AssetTrait.CreatureSkeleton,
                required: AssetTrait.None);
            AssetDescriptor equipment = CreateDescriptor(
                2,
                AssetCategory.Equipment,
                AssetTrait.None,
                required: AssetTrait.HumanoidSkeleton);

            Assert.That(AssetCompatibilityEvaluator.AreCompatible(body, equipment), Is.False);
        }

        [Test]
        public void AreCompatible_ExcludedTraitPresent_ReturnsFalse()
        {
            AssetDescriptor largeBody = CreateDescriptor(
                1,
                AssetCategory.CharacterBody,
                AssetTrait.LargeFrame,
                required: AssetTrait.None);
            AssetDescriptor accessory = CreateDescriptor(
                2,
                AssetCategory.Accessory,
                AssetTrait.None,
                required: AssetTrait.None,
                excluded: AssetTrait.LargeFrame);

            Assert.That(AssetCompatibilityEvaluator.AreCompatible(largeBody, accessory), Is.False);
        }

        [Test]
        public void TryGetDescriptor_KnownId_ReturnsCatalogMetadata()
        {
            AssetDescriptor expected = CreateDescriptor(
                7,
                AssetCategory.Head,
                AssetTrait.HumanoidSkeleton,
                required: AssetTrait.None);
            AssetCatalog catalog = new AssetCatalog(
                new AssetCatalogVersion(1),
                new[] { new AssetCatalogEntry(expected, 5) });

            bool found = catalog.TryGetDescriptor(new AssetId(7), out AssetDescriptor actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static AssetDescriptor CreateDescriptor(
            uint id,
            AssetCategory category,
            AssetTrait traits,
            AssetTrait required,
            AssetTrait excluded = AssetTrait.None)
        {
            return new AssetDescriptor(
                new AssetId(id),
                category,
                traits,
                new AssetCompatibility(required, excluded));
        }
    }
}
