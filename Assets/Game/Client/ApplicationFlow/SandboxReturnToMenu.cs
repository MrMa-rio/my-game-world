using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGameWorld.Client.ApplicationFlow
{
    [DisallowMultipleComponent]
    public sealed class SandboxReturnToMenu : MonoBehaviour
    {
        [SerializeField]
        private ApplicationSceneCatalog _sceneCatalog;

        private bool _loading;

#if UNITY_EDITOR
        public void Configure(ApplicationSceneCatalog sceneCatalog) => _sceneCatalog = sceneCatalog;
#endif

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (_loading || keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            ReturnToMainMenu();
        }

        public void ReturnToMainMenu()
        {
            if (_loading)
            {
                return;
            }

            SceneLoader loader = new SceneLoader(_sceneCatalog);
            _loading = loader.Load(SceneId.MainMenu) != null;
        }
    }
}
