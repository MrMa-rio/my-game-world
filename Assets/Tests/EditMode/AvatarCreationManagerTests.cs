using System.Collections.Generic;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class AvatarCreationManagerTests
    {
        [Test]
        public void CreateImmediately_MaterializesSelectedParts_AndReusesAppearance()
        {
            GameObject managerObject = new GameObject("Manager"); GameObject prefab = new GameObject("BodyPrefab");
            try
            {
                AssetCatalogVersion version = new AssetCatalogVersion(1);
                UnityAssetRegistry registry = new UnityAssetRegistry(version, new[] { new UnityAssetBinding(5001, prefab) });
                CharacterPartDefinition[] definitions = { new CharacterPartDefinition(CharacterPartSlot.Body,
                    new AssetDescriptor(new AssetId(5001), AssetCategory.CharacterBody, AssetTrait.HumanoidSkeleton,
                        new AssetCompatibility(AssetTrait.HumanoidSkeleton, AssetTrait.None))) };
                AvatarCreationManager manager = managerObject.AddComponent<AvatarCreationManager>(); manager.Initialize(registry, definitions);
                RuntimeAvatar first = manager.CreateImmediately(77, AssetTrait.HumanoidSkeleton);
                RuntimeAvatar second = manager.CreateImmediately(77, AssetTrait.HumanoidSkeleton);
                Assert.That(first.PartCount, Is.EqualTo(1)); Assert.That(second.PartCount, Is.EqualTo(1));
                Assert.That(second.Appearance, Is.SameAs(first.Appearance)); Assert.That(manager.Metrics.CacheHits, Is.EqualTo(1));
                manager.Release(first); Assert.That(manager.Metrics.PooledRoots, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(managerObject); Object.DestroyImmediate(prefab); }
        }
    }
}
