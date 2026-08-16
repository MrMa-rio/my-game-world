using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MyGameWorld.Client.ApplicationFlow.Editor
{
    public static class ApplicationFlowSceneBuilder
    {
        public const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string ProceduralWorldScenePath = "Assets/Scenes/ProceduralWorldSandbox.unity";

        private const string CatalogFolder = "Assets/Game/Client/ApplicationFlow/Configuration";
        private const string CatalogPath = CatalogFolder + "/ApplicationSceneCatalog.asset";
        private const string PanelSettingsPath = CatalogFolder + "/MainMenuPanelSettings.asset";

        [MenuItem("My Game World/Build Application Flow Scenes")]
        public static void BuildScenes()
        {
            EnsureFolder(CatalogFolder);
            ApplicationSceneCatalog catalog = CreateOrUpdateCatalog();
            PanelSettings panelSettings = CreateOrUpdatePanelSettings();
            BuildBootstrapScene(catalog);
            BuildMainMenuScene(catalog, panelSettings);
            IntegrateProceduralScene(catalog);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ApplicationFlow] Bootstrap, MainMenu and procedural sandbox navigation are ready.");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildScenes();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static ApplicationSceneCatalog CreateOrUpdateCatalog()
        {
            ApplicationSceneCatalog catalog = AssetDatabase.LoadAssetAtPath<ApplicationSceneCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ApplicationSceneCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(new[]
            {
                new ApplicationSceneCatalog.Entry(SceneId.Bootstrap, "Bootstrap"),
                new ApplicationSceneCatalog.Entry(SceneId.MainMenu, "MainMenu"),
                new ApplicationSceneCatalog.Entry(SceneId.ProceduralWorld, "ProceduralWorldSandbox")
            });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static PanelSettings CreateOrUpdatePanelSettings()
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.name = "Main Menu Panel Settings";
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }

            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void BuildBootstrapScene(ApplicationSceneCatalog catalog)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Application Bootstrap");
            GameBootstrapper bootstrapper = root.AddComponent<GameBootstrapper>();
            bootstrapper.Configure(catalog);
            EditorUtility.SetDirty(bootstrapper);
            SaveScene(scene, BootstrapScenePath);
        }

        private static void BuildMainMenuScene(ApplicationSceneCatalog catalog, PanelSettings panelSettings)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Main Menu");
            UIDocument document = root.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            MainMenuController controller = root.AddComponent<MainMenuController>();
            controller.Configure(catalog);
            EditorUtility.SetDirty(controller);
            SaveScene(scene, MainMenuScenePath);
        }

        private static void IntegrateProceduralScene(ApplicationSceneCatalog catalog)
        {
            Scene scene = EditorSceneManager.OpenScene(ProceduralWorldScenePath, OpenSceneMode.Single);
            SandboxReturnToMenu returnController = UnityEngine.Object.FindAnyObjectByType<SandboxReturnToMenu>();
            if (returnController == null)
            {
                GameObject root = new GameObject("Sandbox Navigation");
                returnController = root.AddComponent<SandboxReturnToMenu>();
            }

            returnController.Configure(catalog);
            EditorUtility.SetDirty(returnController);
            EditorSceneManager.MarkSceneDirty(scene);
            SaveScene(scene, ProceduralWorldScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(ProceduralWorldScenePath, true)
            };
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException($"Could not save {path}.");
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
