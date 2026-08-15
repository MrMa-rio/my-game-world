using System;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class AssetCatalogTests
    {
        [Test]
        public void AssetId_ZeroValue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AssetId(0));
        }

        [Test]
        public void Constructor_DuplicateAssetId_Throws()
        {
            AssetId assetId = new AssetId(1);
            AssetCatalogEntry[] entries =
            {
                new AssetCatalogEntry(assetId, 1),
                new AssetCatalogEntry(assetId, 2)
            };

            Assert.Throws<ArgumentException>(() => CreateCatalog(entries));
        }

        [Test]
        public void Select_KnownCatalogAndSeed_MatchesGoldenSequence()
        {
            AssetCatalog catalog = CreateCatalog(
                new AssetCatalogEntry(new AssetId(1), 1),
                new AssetCatalogEntry(new AssetId(2), 3),
                new AssetCatalogEntry(new AssetId(3), 6));
            uint[] expected = { 2, 2, 3, 3, 1, 2, 3, 3 };
            DeterministicRandom random = new DeterministicRandom(42);

            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(WeightedAssetSelector.Select(catalog, random).Value, Is.EqualTo(expected[index]));
            }
        }

        [Test]
        public void Contains_KnownAndUnknownIds_ReturnsExpectedResult()
        {
            AssetCatalog catalog = CreateCatalog(new AssetCatalogEntry(new AssetId(7), 1));

            Assert.That(catalog.Contains(new AssetId(7)), Is.True);
            Assert.That(catalog.Contains(new AssetId(8)), Is.False);
        }

        private static AssetCatalog CreateCatalog(params AssetCatalogEntry[] entries)
        {
            return new AssetCatalog(new AssetCatalogVersion(1), entries);
        }
    }
}
