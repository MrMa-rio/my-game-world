using System;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Shared.Core;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class UnityAssetRegistryTests
    {
        private GameObject _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = new GameObject("RegistryTestAsset");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_asset);
        }

        [Test]
        public void TryResolve_KnownId_ReturnsUnityObject()
        {
            UnityAssetRegistry registry = CreateRegistry(new UnityAssetBinding(5, _asset));

            bool resolved = registry.TryResolve(new AssetId(5), out UnityEngine.Object actual);

            Assert.That(resolved, Is.True);
            Assert.That(actual, Is.SameAs(_asset));
        }

        [Test]
        public void Constructor_DuplicateId_Throws()
        {
            UnityAssetBinding[] bindings =
            {
                new UnityAssetBinding(5, _asset),
                new UnityAssetBinding(5, _asset)
            };

            Assert.Throws<ArgumentException>(() => CreateRegistry(bindings));
        }

        [Test]
        public void Constructor_MissingUnityObject_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateRegistry(new UnityAssetBinding(5, null)));
        }

        [Test]
        public void Constructor_ZeroAssetId_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateRegistry(new UnityAssetBinding(0, _asset)));
        }

        private static UnityAssetRegistry CreateRegistry(params UnityAssetBinding[] bindings)
        {
            return new UnityAssetRegistry(new AssetCatalogVersion(1), bindings);
        }
    }
}
