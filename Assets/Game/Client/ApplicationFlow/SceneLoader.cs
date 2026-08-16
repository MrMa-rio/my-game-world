using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameWorld.Client.ApplicationFlow
{
    public sealed class SceneLoader
    {
        private readonly ApplicationSceneCatalog _catalog;

        public SceneLoader(ApplicationSceneCatalog catalog)
        {
            _catalog = catalog;
        }

        public AsyncOperation Load(SceneId sceneId)
        {
            if (_catalog == null || !_catalog.TryGetSceneName(sceneId, out string sceneName))
            {
                Debug.LogError($"[SceneLoader] SceneId '{sceneId}' is not registered in the application scene catalog.");
                return null;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Cannot load SceneId '{sceneId}'. Expected build scene: '{sceneName}'.");
                return null;
            }

            Debug.Log($"[SceneLoader] Loading {sceneId} ({sceneName})...");
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }
}
