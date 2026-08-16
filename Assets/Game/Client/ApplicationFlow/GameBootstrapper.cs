using System.Collections;
using UnityEngine;

namespace MyGameWorld.Client.ApplicationFlow
{
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private ApplicationSceneCatalog _sceneCatalog;

        public ApplicationFlowState State { get; private set; } = ApplicationFlowState.Booting;

#if UNITY_EDITOR
        public void Configure(ApplicationSceneCatalog sceneCatalog) => _sceneCatalog = sceneCatalog;
#endif

        private IEnumerator Start()
        {
            Debug.Log("[Bootstrap] Started");
            SceneLoader loader = new SceneLoader(_sceneCatalog);
            Debug.Log("[Bootstrap] Ready");
            State = ApplicationFlowState.Loading;
            AsyncOperation operation = loader.Load(SceneId.MainMenu);
            if (operation != null)
            {
                yield return operation;
            }
        }
    }
}
