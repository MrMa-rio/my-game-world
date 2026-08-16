using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class SystemG6ModularAvatarValidationTests
    {
        private const string Root = "Assets/ia assets/avatar-reference/system-g6/normalized/parts/";

        [TestCase("male", 30)]
        [TestCase("female", 24)]
        public void NormalizedFixture_PreservesSeparateSkinnedParts(string group, int minimumParts)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { Root + group });
            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(minimumParts));
            foreach (string guid in guids)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                SkinnedMeshRenderer renderer = asset.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.That(renderer, Is.Not.Null); Assert.That(renderer.sharedMesh, Is.Not.Null); Assert.That(renderer.bones, Is.Not.Empty);
            }
        }

        [Test]
        public void NormalizedMaleFixture_ContainsExpectedSwapCategories()
        {
            string[] names = AssetDatabase.FindAssets("t:Model", new[] { Root + "male" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(System.IO.Path.GetFileNameWithoutExtension).ToArray();
            Assert.That(names.Any(name => name.Contains("head")), Is.True);
            Assert.That(names.Any(name => name.Contains("hair")), Is.True);
            Assert.That(names.Any(name => name.Contains("armor")), Is.True);
            Assert.That(names.Any(name => name.Contains("boots")), Is.True);
            Assert.That(names.Any(name => name.Contains("gloves")), Is.True);
        }
    }
}
