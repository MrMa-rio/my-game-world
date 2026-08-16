using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;

namespace MyGameWorld.Client.ProceduralWorld.Editor
{
    public static class ProceduralWorldPlayerIntegrator
    {
        [MenuItem("My Game World/Integrate Player Into Procedural World")]
        public static void Integrate()
        {
            Scene scene = EditorSceneManager.OpenScene(ProceduralWorldSandboxSceneBuilder.ScenePath, OpenSceneMode.Single);
            ProceduralWorldPlayerCoordinator coordinator = UnityEngine.Object.FindAnyObjectByType<ProceduralWorldPlayerCoordinator>();
            if (coordinator == null)
            {
                coordinator = new GameObject("Procedural Player Integration").AddComponent<ProceduralWorldPlayerCoordinator>();
            }

            coordinator.Configure(
                AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"),
                Vector2.zero,
                AssetDatabase.LoadAssetAtPath<UnityAssetCatalog>("Assets/Game/Content/AvatarValidation/SystemG6UnityAssetCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<AvatarPartCatalog>("Assets/Game/Content/AvatarValidation/SystemG6AvatarPartCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HumanBasicMotionsIntegrator.ControllerPath));
            EditorUtility.SetDirty(coordinator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ProceduralWorldSandboxSceneBuilder.ScenePath))
            {
                throw new InvalidOperationException("Could not save the procedural world player integration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[ProceduralPlayer] Existing procedural scene integrated without rebuilding it.");
        }

        public static void IntegrateFromCommandLine()
        {
            try
            {
                Integrate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void OpenSandboxAndPlayFromCommandLine()
        {
            EditorSceneManager.OpenScene(ProceduralWorldSandboxSceneBuilder.ScenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            Debug.Log("[ProceduralWorld] Opened sandbox and scheduled Play Mode from command line.");
        }
    }
}
