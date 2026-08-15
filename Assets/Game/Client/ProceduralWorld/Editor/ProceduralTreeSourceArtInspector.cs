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
    public static class ProceduralTreeSourceArtInspector
    {
        private const string SourcePath = "Assets/ia assets/Meshy_AI_Geometric_Tree_0815191238_texture.glb";
        private const string CapturePath = "Logs/TreeSourceReference.png";

        public static void CaptureFromCommandLine()
        {
            try
            {
                Capture();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureProceduralFromCommandLine()
        {
            try
            {
                CaptureProceduralGallery();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("My Game World/Source Art/Capture Tree Reference")]
        public static void Capture()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null) throw new InvalidOperationException($"glTF source art was not imported: {SourcePath}");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject instance = UnityEngine.Object.Instantiate(source);
            instance.name = "Tree Source Reference";
            Bounds bounds = CalculateBounds(instance);
            instance.transform.position -= bounds.center;

            GameObject cameraObject = new GameObject("Source Art Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.76f, 0.84f, 0.88f);
            camera.fieldOfView = 32f;
            float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            camera.transform.position = new Vector3(extent * 2.5f, extent * 0.65f, -extent * 4.4f);
            camera.transform.LookAt(Vector3.zero);

            GameObject lightObject = new GameObject("Source Art Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.92f, 0.8f);
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.54f, 0.58f);

            RenderTexture target = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            Texture2D capture = new Texture2D(768, 768, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                capture.Apply();
                string absolutePath = Path.GetFullPath(CapturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, capture.EncodeToPNG());
                Debug.Log($"Captured source tree ({CountTriangles(instance):N0} triangles) to {absolutePath}.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }

        [MenuItem("My Game World/Source Art/Capture Procedural Tree Gallery")]
        public static void CaptureProceduralGallery()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProceduralWorldMaterialLibrary materials = new ProceduralWorldMaterialLibrary();
            GameObject host = new GameObject("Procedural Tree Runtime Gallery");
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
                        variation, DecorationKind.Tree, WorldVisualAssetIds.TemperateTree,
                        new WorldVector3((variation - 1.5f) * 5.2f, 0f, 0f), variation * 11f, 1f);
                    manager.Request(new ProceduralGenerationRequest(definition,
                        new ProceduralEnvironmentContext(BiomeId.TemperateGrassland, Vector3.up, 0f, 0f),
                        ProceduralVisualLod.High, GenerationPriority.High));
                }
                manager.FlushQueue();

                GameObject cameraObject = new GameObject("Procedural Gallery Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.76f, 0.84f, 0.88f);
                camera.fieldOfView = 34f;
                camera.transform.position = new Vector3(0f, 3.1f, -24f);
                camera.transform.LookAt(new Vector3(0f, 2.7f, 0f));

                GameObject lightObject = new GameObject("Gallery Key Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.92f, 0.8f);
                light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
                RenderSettings.sun = light;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.46f, 0.65f, 0.82f);
                RenderSettings.ambientEquatorColor = new Color(0.40f, 0.48f, 0.42f);
                RenderSettings.ambientGroundColor = new Color(0.22f, 0.25f, 0.20f);
                RenderSettings.fog = false;
                CaptureCamera(camera, "Logs/ProceduralTreeGallery.png");
                Debug.Log("Captured procedural tree gallery.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                materials.Dispose();
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Source art contains no renderer.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static long CountTriangles(GameObject root)
        {
            long count = 0;
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
            for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
            {
                Mesh mesh = filters[filterIndex].sharedMesh;
                if (mesh == null) continue;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++) count += (long)mesh.GetIndexCount(submesh) / 3L;
            }
            return count;
        }

        private static void CaptureCamera(Camera camera, string relativePath)
        {
            RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D capture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                capture.Apply();
                string absolutePath = Path.GetFullPath(relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, capture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
