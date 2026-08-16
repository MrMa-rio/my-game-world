using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyGameWorld.Client.ProceduralWorld;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Client.CharacterRuntime;

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
            ProceduralWorldPlayerCoordinator playerCoordinator = Object.FindAnyObjectByType<ProceduralWorldPlayerCoordinator>();
            Assert.That(playerCoordinator, Is.Not.Null);
            for (int frame = 0; frame < 120 && playerCoordinator.PlayerRuntime == null; frame++) yield return null;
            Assert.That(playerCoordinator.PlayerRuntime?.PlayerActor, Is.Not.Null);
            Assert.That(playerCoordinator.PlayerRuntime.Avatar, Is.Not.Null);
            Assert.That(playerCoordinator.PlayerRuntime.Avatar.PartCount, Is.GreaterThan(3));
            Assert.That(GameObject.Find("Player Scale Reference"), Is.Null);
            HumanoidMotionAnimation humanoidMotion = playerCoordinator.PlayerRuntime.HumanoidMotion;
            Assert.That(humanoidMotion, Is.Not.Null);
            Assert.That(humanoidMotion.IsOperational, Is.True);
            Assert.That(humanoidMotion.AnimatorCount, Is.EqualTo(1));
            Assert.That(humanoidMotion.ReboundRendererCount, Is.GreaterThan(1));
            Assert.That(humanoidMotion.MappedBoneCount, Is.GreaterThan(20));
            Assert.That(humanoidMotion.DisabledDuplicateAnimatorCount, Is.GreaterThan(0));
            Assert.That(playerCoordinator.PlayerRuntime.Avatar.GetComponent<ProceduralAvatarAnimation>(), Is.Null);
            Assert.That(playerCoordinator.PlayerRuntime.AnimationDriver, Is.Not.Null);
            Assert.That(playerCoordinator.PlayerRuntime.CameraSystem.Modes.ActiveMode.Id, Is.EqualTo(PlayerCameraModeId.FirstPerson));
            CharacterController playerController = playerCoordinator.PlayerRuntime.PlayerActor.GetComponent<CharacterController>();
            Assert.That(playerController, Is.Not.Null);
            Assert.That(playerController.gameObject.layer, Is.EqualTo(WorldPhysicsLayers.Actor));
            MeshCollider terrainCollider = Object.FindAnyObjectByType<MeshCollider>();
            Assert.That(terrainCollider, Is.Not.Null);
            Assert.That(terrainCollider.gameObject.layer, Is.EqualTo(WorldPhysicsLayers.Terrain));
            CapsuleCollider solidTree = Object.FindObjectsByType<CapsuleCollider>()
                .FirstOrDefault(collider => collider.enabled && collider.gameObject.layer == WorldPhysicsLayers.StaticWorld);
            BoxCollider softDecoration = Object.FindObjectsByType<BoxCollider>()
                .FirstOrDefault(collider => collider.enabled && collider.isTrigger && collider.gameObject.layer == WorldPhysicsLayers.SoftEnvironment);
            Assert.That(solidTree, Is.Not.Null);
            Assert.That(softDecoration, Is.Not.Null);
            Assert.That(playerCoordinator.PlayerRuntime.PlayerActor.Capabilities.TryGet(out IActorLocomotion playerLocomotion), Is.True);
            for (int frame = 0; frame < 30 && !playerLocomotion.State.IsGrounded; frame++) yield return new WaitForFixedUpdate();
            Assert.That(playerLocomotion.State.IsGrounded, Is.True);
            HumanActorController humanController = playerCoordinator.PlayerRuntime.PlayerActor.GetComponent<HumanActorController>();
            humanController.enabled = false;
            humanController.ProcessInput(new HumanInputSnapshot(Vector2.up, Vector2.zero, false, false, false));
            for (int frame = 0; frame < 6; frame++) yield return new WaitForFixedUpdate();
            Assert.That(playerCoordinator.PlayerRuntime.AnimationDriver.Current.Movement, Is.EqualTo(ActorAnimationMovementState.Walk));
            humanController.ProcessInput(new HumanInputSnapshot(Vector2.zero, Vector2.zero, false, false, false));
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
            Assert.That(sandbox.RuntimeMetrics.GeneratedMeshes, Is.GreaterThanOrEqualTo(generatedBefore));
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

        [UnityTest]
        public IEnumerator ImageStability_MovingCameraAndQualityChanges_RemainOperational()
        {
            SceneManager.LoadScene("ProceduralWorldSandbox");
            yield return null;
            ProceduralWorldSandbox sandbox = Object.FindAnyObjectByType<ProceduralWorldSandbox>();
            RenderingQualityManager quality = Object.FindAnyObjectByType<RenderingQualityManager>();
            Camera camera = Camera.main;
            Assert.That(sandbox, Is.Not.Null); Assert.That(quality, Is.Not.Null); Assert.That(camera, Is.Not.Null);
            for (int frame = 0; frame < 24; frame++)
            {
                camera.transform.position += camera.transform.forward * 6f;
                camera.transform.Rotate(0f, 1.5f, 0f, Space.World);
                if (frame == 6 || frame == 12 || frame == 18) sandbox.CycleRenderingQuality();
                yield return null;
            }
            RenderingStabilityMetrics metrics = sandbox.RenderingMetrics;
            Assert.That(metrics.Width, Is.GreaterThan(0));
            Assert.That(metrics.Height, Is.GreaterThan(0));
            Assert.That(metrics.RenderScale, Is.InRange(0.8f, 1f));
            sandbox.CycleAntiAliasing(); yield return null;
            Assert.That(sandbox.RenderingMetrics.Mode, Is.Not.EqualTo(metrics.Mode));
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
