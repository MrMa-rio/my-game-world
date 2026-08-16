using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MyGameWorld.Client.ApplicationFlow;
using UnityEngine.InputSystem;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;

namespace MyGameWorld.Client.ProceduralWorld.Editor
{
    public static class ProceduralWorldSandboxSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ProceduralWorldSandbox.unity";
        private const string SkyboxPath = "Assets/Settings/ProceduralWorldSkybox.mat";
        private const string QualityFolder = "Assets/Resources/RenderingQuality";

        [MenuItem("My Game World/Build Procedural World Sandbox Scene")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ProceduralWorldSandbox";

            GameObject world = new GameObject("Procedural World Sandbox");
            world.AddComponent<ProceduralWorldSandbox>();
            world.AddComponent<ProceduralWorldDebugHud>();
            RenderingQualityManager quality = world.AddComponent<RenderingQualityManager>();
            quality.ConfigureProfiles(CreateRenderingProfiles(), RenderingQualityTier.High);

            GameObject cameraObject = new GameObject("Development Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<DevelopmentFreeCamera>();
            camera.transform.position = new Vector3(0f, 260f, -330f);
            camera.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 52500f;
            camera.fieldOfView = 62f;
            camera.allowHDR = true;

            GameObject lightObject = new GameObject("Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.91f, 0.76f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            ApplicationSceneCatalog sceneCatalog = AssetDatabase.LoadAssetAtPath<ApplicationSceneCatalog>(
                "Assets/Game/Client/ApplicationFlow/Configuration/ApplicationSceneCatalog.asset");
            if (sceneCatalog != null)
            {
                GameObject navigation = new GameObject("Sandbox Navigation");
                SandboxReturnToMenu returnToMenu = navigation.AddComponent<SandboxReturnToMenu>();
                returnToMenu.Configure(sceneCatalog);
            }

            GameObject playerIntegration = new GameObject("Procedural Player Integration");
            ProceduralWorldPlayerCoordinator playerCoordinator = playerIntegration.AddComponent<ProceduralWorldPlayerCoordinator>();
            playerCoordinator.Configure(
                AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"),
                Vector2.zero,
                AssetDatabase.LoadAssetAtPath<UnityAssetCatalog>("Assets/Game/Content/AvatarValidation/SystemG6UnityAssetCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<AvatarPartCatalog>("Assets/Game/Content/AvatarValidation/SystemG6AvatarPartCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HumanBasicMotionsIntegrator.ControllerPath));

            ConfigureEnvironment(sun);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Could not save {ScenePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = world;
            Debug.Log($"Built {ScenePath}. Application startup remains owned by the application flow builder.");
        }

        private static RenderingQualityProfile[] CreateRenderingProfiles()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(QualityFolder)) AssetDatabase.CreateFolder("Assets/Resources", "RenderingQuality");
            return new[] {
                GetOrCreateProfile("Low", RenderingQualityTier.Low, ImageAntiAliasingMode.Fxaa, 1, 0.8f, false,
                    AnisotropicFiltering.Enable, 1f, 0f, false, true, 2.5f),
                GetOrCreateProfile("Medium", RenderingQualityTier.Medium, ImageAntiAliasingMode.Smaa, 1, 0.9f, false,
                    AnisotropicFiltering.ForceEnable, 1.25f, -0.1f, false, true, 2f),
                GetOrCreateProfile("High", RenderingQualityTier.High, ImageAntiAliasingMode.Smaa, 1, 1f, true,
                    AnisotropicFiltering.ForceEnable, 1.6f, -0.15f, false, true, 1.5f),
                GetOrCreateProfile("Ultra", RenderingQualityTier.Ultra, ImageAntiAliasingMode.Temporal, 1, 1f, true,
                    AnisotropicFiltering.ForceEnable, 2f, -0.25f, false, true, 1f, 0.9f, 0.22f)
            };
        }

        private static RenderingQualityProfile GetOrCreateProfile(string name, RenderingQualityTier tier,
            ImageAntiAliasingMode aa, int msaa, float renderScale, bool temporal, AnisotropicFiltering anisotropic,
            float lodBias, float mipBias, bool alphaToCoverage, bool distantStability, float subpixel,
            float history = 0.88f, float sharpen = 0.2f)
        {
            string path = $"{QualityFolder}/{name}.asset";
            RenderingQualityProfile profile = AssetDatabase.LoadAssetAtPath<RenderingQualityProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<RenderingQualityProfile>(); profile.name = name; AssetDatabase.CreateAsset(profile, path); }
            profile.Configure(tier, aa, msaa, renderScale, temporal, anisotropic, lodBias, mipBias,
                alphaToCoverage, distantStability, subpixel, history, sharpen);
            EditorUtility.SetDirty(profile); return profile;
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildScene();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureEnvironment(Light sun)
        {
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skybox == null)
            {
                Shader skyShader = Shader.Find("Skybox/Procedural");
                if (skyShader == null)
                {
                    throw new InvalidOperationException("Unity procedural skybox shader was not found.");
                }

                skybox = new Material(skyShader) { name = "Procedural World Skybox" };
                skybox.SetColor("_SkyTint", new Color(0.38f, 0.67f, 0.88f));
                skybox.SetColor("_GroundColor", new Color(0.42f, 0.48f, 0.38f));
                skybox.SetFloat("_AtmosphereThickness", 0.82f);
                skybox.SetFloat("_Exposure", 1.15f);
                AssetDatabase.CreateAsset(skybox, SkyboxPath);
            }

            RenderSettings.skybox = skybox;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.65f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.48f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.25f, 0.20f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.61f, 0.75f, 0.82f);
            RenderSettings.fogStartDistance = 30000f;
            RenderSettings.fogEndDistance = 50000f;
        }
    }
}
