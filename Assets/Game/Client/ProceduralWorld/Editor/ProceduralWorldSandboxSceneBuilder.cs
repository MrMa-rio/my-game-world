using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MyGameWorld.Client.ProceduralWorld.Editor
{
    public static class ProceduralWorldSandboxSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ProceduralWorldSandbox.unity";
        private const string SkyboxPath = "Assets/Settings/ProceduralWorldSkybox.mat";

        [MenuItem("My Game World/Build Procedural World Sandbox Scene")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ProceduralWorldSandbox";

            GameObject world = new GameObject("Procedural World Sandbox");
            world.AddComponent<ProceduralWorldSandbox>();
            world.AddComponent<ProceduralWorldDebugHud>();

            GameObject cameraObject = new GameObject("Development Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<DevelopmentFreeCamera>();
            camera.transform.position = new Vector3(0f, 210f, -330f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1800f;
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

            ConfigureEnvironment(sun);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Could not save {ScenePath}.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = world;
            Debug.Log($"Built {ScenePath} and set it as the startup scene.");
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
            RenderSettings.fogStartDistance = 450f;
            RenderSettings.fogEndDistance = 1250f;
        }
    }
}
