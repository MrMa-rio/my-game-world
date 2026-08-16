using System;
using System.IO;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.ProceduralWorld.Editor
{
    public static class ProceduralRockArtInspector
    {
        public static void CaptureFromCommandLine()
        {
            try { CaptureGallery(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("My Game World/Source Art/Capture Procedural Rock Gallery")]
        public static void CaptureGallery()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProceduralWorldMaterialLibrary materials = new ProceduralWorldMaterialLibrary();
            GameObject host = new GameObject("Procedural Rock Runtime Gallery");
            ProceduralRuntimeManager manager = host.AddComponent<ProceduralRuntimeManager>();
            manager.Initialize(materials);
            manager.SetInstanceParent(host.transform);
            try
            {
                ZoneDNA zone = new ZoneDNA(new ZoneId(1), 42, BiomeId.TemperateGrassland,
                    TerrainProfileId.RollingLowPoly, TerrainGeneratorV2.GeneratorVersion, new AssetCatalogVersion(1));
                for (byte variation = 0; variation < 4; variation++)
                {
                    DecorationPlacement definition = new DecorationPlacement(new WorldElementId(variation + 1), zone,
                        variation, DecorationKind.Rock, WorldVisualAssetIds.TemperateRock,
                        new WorldVector3((variation - 1.5f) * 3.4f, 0f, 0f), variation * 17f, 1f);
                    manager.Request(new ProceduralGenerationRequest(definition,
                        new ProceduralEnvironmentContext(BiomeId.TemperateGrassland, Vector3.up, 0f, 0f),
                        ProceduralVisualLod.High, GenerationPriority.High));
                }
                manager.FlushQueue();

                GameObject cameraObject = new GameObject("Rock Gallery Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.76f, 0.84f, 0.88f);
                camera.fieldOfView = 31f;
                camera.transform.position = new Vector3(0f, 1.25f, -14f);
                camera.transform.LookAt(new Vector3(0f, 0.72f, 0f));

                GameObject lightObject = new GameObject("Rock Gallery Key Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.91f, 0.76f);
                light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                RenderSettings.sun = light;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.46f, 0.65f, 0.82f);
                RenderSettings.ambientEquatorColor = new Color(0.40f, 0.48f, 0.42f);
                RenderSettings.ambientGroundColor = new Color(0.22f, 0.25f, 0.20f);
                RenderSettings.fog = false;
                Capture(camera);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                materials.Dispose();
            }
        }

        private static void Capture(Camera camera)
        {
            RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D capture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0); capture.Apply();
                string path = Path.GetFullPath("Logs/ProceduralRockGallery.png");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, capture.EncodeToPNG());
                Debug.Log($"Captured procedural rock gallery to {path}.");
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
