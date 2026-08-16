using System.Collections;
using System.Collections.Generic;
using System.IO;
using MyGameWorld.Client.ProceduralWorld;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameWorld.Tests.PlayMode
{
    public sealed class ProceduralWorldSandboxTests
    {
        [UnityTest]
        public IEnumerator Start_SandboxScene_GeneratesExpectedRuntimeZone()
        {
            SceneManager.LoadScene("ProceduralWorldSandbox");
            yield return null;

            ProceduralWorldSandbox sandbox = UnityEngine.Object.FindAnyObjectByType<ProceduralWorldSandbox>();
            Assert.That(sandbox, Is.Not.Null);
            for (int frame = 0; frame < 240 && sandbox.RuntimeMetrics.QueueCount > 0; frame++)
            {
                yield return null;
            }
            Assert.That(sandbox.Fingerprint, Is.Not.Zero);
            Assert.That(sandbox.TerrainWidth, Is.EqualTo(1000f));
            Assert.That(sandbox.TerrainDepth, Is.EqualTo(1000f));
            Assert.That(sandbox.ChunkCount, Is.EqualTo(100));
            Assert.That(sandbox.TriangleCount, Is.EqualTo(80000));
            Assert.That(sandbox.RenderedVertexCount, Is.EqualTo(240000));
            Assert.That(sandbox.DecorationCount, Is.EqualTo(sandbox.PlannedDecorationCount));
            Assert.That(sandbox.RuntimeMetrics.QueueCount, Is.Zero);
            Assert.That(sandbox.RuntimeMetrics.CachedMeshes, Is.LessThan(sandbox.DecorationCount));
            Assert.That(sandbox.RuntimeMetrics.CacheHits, Is.GreaterThan(0));
            EnvironmentalManager environment = UnityEngine.Object.FindAnyObjectByType<EnvironmentalManager>();
            Assert.That(environment, Is.Not.Null);
            Assert.That(environment.PhysicalResponses.RegisteredCount, Is.GreaterThan(0));
            Assert.That(UnityEngine.Object.FindObjectsByType<Rigidbody>().Length, Is.Zero);
            Assert.That(UnityEngine.Object.FindObjectsByType<ParticleSystem>().Length, Is.LessThanOrEqualTo(12));
            Assert.That(environment.TimeSystem.Snapshot.Hour, Is.InRange(0f, 24f));
            Assert.That(RenderSettings.sun, Is.Not.Null);
            Assert.That(GameObject.Find("Moon"), Is.Not.Null);
            Assert.That(GameObject.Find("Procedural Star Field"), Is.Not.Null);
            Assert.That(sandbox.ProceduralStarCount, Is.GreaterThan(10000));
            Assert.That(sandbox.ProceduralStars[0].Kind, Is.EqualTo(CelestialItemKind.Star));
            Assert.That(sandbox.ProceduralStars[0].ItemId, Is.EqualTo(1));
            Assert.That(sandbox.ProceduralStars[0].Seed, Is.Not.Zero);
            Assert.That(environment.GetComponentsInChildren<TrailRenderer>(true).Length, Is.EqualTo(4));
            Assert.That(sandbox.SingularTerrainFeatureCount, Is.GreaterThan(20));
            WorldElementRuntimeIdentity[] identities = UnityEngine.Object.FindObjectsByType<WorldElementRuntimeIdentity>();
            Assert.That(identities.Length, Is.EqualTo(sandbox.DecorationCount + sandbox.ChunkCount + 1));

            MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>();
            HashSet<Mesh> decorationMeshes = new HashSet<Mesh>();
            int proceduralRenderers = 0;
            for (int index = 0; index < filters.Length; index++)
            {
                WorldElementRuntimeIdentity identity = filters[index].GetComponent<WorldElementRuntimeIdentity>();
                if (identity == null || identity.Kind == WorldElementKind.TerrainSurface || identity.Kind == WorldElementKind.LiquidBody) continue;
                proceduralRenderers++;
                decorationMeshes.Add(filters[index].sharedMesh);
                Assert.That(filters[index].GetComponent<MeshCollider>(), Is.Null);
            }
            Assert.That(proceduralRenderers, Is.EqualTo(sandbox.DecorationCount));
            Assert.That(decorationMeshes.Count, Is.LessThan(proceduralRenderers));

            int generatedBefore = sandbox.RuntimeMetrics.GeneratedMeshes;
            int hitsBefore = sandbox.RuntimeMetrics.CacheHits;
            ulong fingerprintBefore = sandbox.Fingerprint;
            sandbox.RegenerateSameSeed();
            for (int frame = 0; frame < 240 && sandbox.RuntimeMetrics.QueueCount > 0; frame++) yield return null;
            Assert.That(sandbox.Fingerprint, Is.EqualTo(fingerprintBefore));
            Assert.That(sandbox.RuntimeMetrics.GeneratedMeshes, Is.EqualTo(generatedBefore));
            Assert.That(sandbox.RuntimeMetrics.CacheHits, Is.GreaterThan(hitsBefore));

            if (System.Environment.GetEnvironmentVariable("MY_GAME_WORLD_CAPTURE_SANDBOX") == "1")
            {
                string requestedHour = System.Environment.GetEnvironmentVariable("MY_GAME_WORLD_CAPTURE_HOUR");
                if (float.TryParse(requestedHour, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float hour))
                {
                    sandbox.SetWorldHour(hour); yield return null;
                }
                CaptureSandbox(sandbox);
            }
        }

        private static void CaptureSandbox(ProceduralWorldSandbox sandbox)
        {
            const int width = 1280;
            const int height = 720;
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                screenshot.Apply();
                string suffix = sandbox.WorldTime.Hour.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', '-');
                string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", $"ProceduralWorldSandbox-{suffix}h.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
                Debug.Log($"Captured procedural world sandbox to {outputPath}.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.Destroy(renderTexture);
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }
}
