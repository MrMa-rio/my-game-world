using System.Linq;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;
using UnityEditor;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class AvatarFixtureGalleryTests
    {
        [Test]
        public void GeneratedCatalogs_ContainAllNormalizedParts_AndGenerateBothFamilies()
        {
            UnityAssetCatalog assets = AssetDatabase.LoadAssetAtPath<UnityAssetCatalog>(
                "Assets/Game/Content/AvatarValidation/SystemG6UnityAssetCatalog.asset");
            AvatarPartCatalog parts = AssetDatabase.LoadAssetAtPath<AvatarPartCatalog>(
                "Assets/Game/Content/AvatarValidation/SystemG6AvatarPartCatalog.asset");
            Assert.That(assets, Is.Not.Null); Assert.That(parts, Is.Not.Null);
            Assert.That(assets.Bindings.Count, Is.EqualTo(58)); Assert.That(parts.Count, Is.EqualTo(58));
            CharacterAppearanceGenerator generator = new CharacterAppearanceGenerator();
            CharacterAppearanceDNA masculine = generator.Generate(100, assets.Version,
                AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame | AssetTrait.MasculineFrame, parts.CreateDefinitions());
            CharacterAppearanceDNA feminine = generator.Generate(100, assets.Version,
                AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame | AssetTrait.FeminineFrame, parts.CreateDefinitions());
            Assert.That(masculine.Parts, Is.Not.Empty); Assert.That(feminine.Parts, Is.Not.Empty);
            Assert.That(masculine.Parts.Select(part => part.AssetId), Is.Not.EqualTo(feminine.Parts.Select(part => part.AssetId)));
        }

        [Test]
        public void GalleryScene_Exists()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/AvatarFixtureGallery.unity"), Is.Not.Null);
        }
    }
}
