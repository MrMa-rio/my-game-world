using MyGameWorld.Client.ApplicationFlow;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ApplicationSceneCatalogTests
    {
        [Test]
        public void TryGetSceneName_RegisteredScene_ReturnsCentralMapping()
        {
            ApplicationSceneCatalog catalog = ScriptableObject.CreateInstance<ApplicationSceneCatalog>();
            catalog.Configure(new[]
            {
                new ApplicationSceneCatalog.Entry(SceneId.ProceduralWorld, "ProceduralWorldSandbox")
            });

            bool found = catalog.TryGetSceneName(SceneId.ProceduralWorld, out string sceneName);

            Assert.That(found, Is.True);
            Assert.That(sceneName, Is.EqualTo("ProceduralWorldSandbox"));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void TryGetSceneName_UnregisteredScene_ReturnsFalse()
        {
            ApplicationSceneCatalog catalog = ScriptableObject.CreateInstance<ApplicationSceneCatalog>();

            bool found = catalog.TryGetSceneName(SceneId.MainMenu, out string sceneName);

            Assert.That(found, Is.False);
            Assert.That(sceneName, Is.Empty);
            Object.DestroyImmediate(catalog);
        }
    }
}
