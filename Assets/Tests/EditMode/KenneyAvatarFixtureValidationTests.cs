using System.Linq;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class KenneyAvatarFixtureValidationTests
    {
        private const string ModelPath = "Assets/ia assets/avatar/Model/characterMedium.fbx";

        [Test]
        public void CharacterMedium_IsRiggedRenderableFixture_AndMaterializesAsBodySlot()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Assert.That(source, Is.Not.Null, "The CC0 validation FBX must remain importable.");
            Assert.That(source.GetComponentsInChildren<SkinnedMeshRenderer>(true), Is.Not.Empty,
                "The fixture must contain skinned geometry.");
            Assert.That(source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(renderer => renderer.bones.Length > 0), Is.True,
                "The fixture must preserve a deforming skeleton.");

            GameObject managerObject = new GameObject("Avatar Fixture Manager");
            try
            {
                AssetCatalogVersion version = new AssetCatalogVersion(1);
                UnityAssetRegistry registry = new UnityAssetRegistry(version, new[] { new UnityAssetBinding(51001, source) });
                CharacterPartDefinition[] definitions =
                {
                    new CharacterPartDefinition(CharacterPartSlot.Body,
                        new AssetDescriptor(new AssetId(51001), AssetCategory.CharacterBody,
                            AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame,
                            new AssetCompatibility(AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame, AssetTrait.None)))
                };
                AvatarCreationManager manager = managerObject.AddComponent<AvatarCreationManager>(); manager.Initialize(registry, definitions);
                RuntimeAvatar avatar = manager.CreateImmediately(20260816,
                    AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame, managerObject.transform);
                Assert.That(avatar.PartCount, Is.EqualTo(1));
                Assert.That(avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true), Is.Not.Empty);
            }
            finally { Object.DestroyImmediate(managerObject); }
        }

        [Test]
        public void Fixture_ContainsExpectedSkinsAndAnimationSources()
        {
            string[] skins = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ia assets/avatar/Skins" });
            Assert.That(skins.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ia assets/avatar/Animations/idle.fbx"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ia assets/avatar/Animations/run.fbx"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ia assets/avatar/Animations/jump.fbx"), Is.Not.Null);
        }
    }
}
