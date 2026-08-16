using System.Collections;
using MyGameWorld.Client.ApplicationFlow;
using MyGameWorld.Client.ProceduralWorld;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MyGameWorld.Client.PlayerRuntime;

namespace MyGameWorld.Tests.PlayMode
{
    public sealed class ApplicationFlowTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_Menu_Sandbox_ReturnAndReload_PreservesProceduralResult()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return WaitForScene("MainMenu", 120);

            Assert.That(Object.FindObjectsByType<GameBootstrapper>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<UIDocument>().Length, Is.EqualTo(1));
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.State, Is.EqualTo(ApplicationFlowState.MainMenu));
            menu.ShowDeveloperMenu();
            Assert.That(menu.State, Is.EqualTo(ApplicationFlowState.Development));
            menu.LaunchProceduralWorld();
            yield return WaitForScene("ProceduralWorldSandbox", 360);

            ProceduralWorldSandbox firstSandbox = Object.FindAnyObjectByType<ProceduralWorldSandbox>();
            Assert.That(firstSandbox, Is.Not.Null);
            yield return WaitForGeneration(firstSandbox, 360);
            ulong expectedFingerprint = firstSandbox.Fingerprint;
            Assert.That(expectedFingerprint, Is.Not.Zero);
            Assert.That(Object.FindObjectsByType<SandboxReturnToMenu>().Length, Is.EqualTo(1));
            ProceduralWorldPlayerCoordinator firstPlayer = Object.FindAnyObjectByType<ProceduralWorldPlayerCoordinator>();
            Assert.That(firstPlayer, Is.Not.Null);
            for (int frame = 0; frame < 120 && firstPlayer.PlayerRuntime == null; frame++) yield return null;
            Assert.That(firstPlayer.PlayerRuntime?.PlayerActor, Is.Not.Null);
            Assert.That(firstPlayer.PlayerRuntime.CameraSystem.Modes.ActiveMode.Id, Is.EqualTo(PlayerCameraModeId.FirstPerson));
            Assert.That(Camera.main?.GetComponent<PlayerCameraSystem>(), Is.Not.Null);
            Assert.That(firstPlayer.ReplacedDevelopmentCamera, Is.Not.Null);
            Assert.That(firstPlayer.ReplacedDevelopmentCamera.gameObject.activeSelf, Is.False);

            Object.FindAnyObjectByType<SandboxReturnToMenu>().ReturnToMainMenu();
            yield return WaitForScene("MainMenu", 120);
            Assert.That(Object.FindObjectsByType<UIDocument>().Length, Is.EqualTo(1));
            menu = Object.FindAnyObjectByType<MainMenuController>();
            menu.ShowDeveloperMenu();
            menu.LaunchProceduralWorld();
            yield return WaitForScene("ProceduralWorldSandbox", 360);

            ProceduralWorldSandbox secondSandbox = Object.FindAnyObjectByType<ProceduralWorldSandbox>();
            yield return WaitForGeneration(secondSandbox, 360);
            Assert.That(secondSandbox.Fingerprint, Is.EqualTo(expectedFingerprint));
            Assert.That(Object.FindObjectsByType<SandboxReturnToMenu>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<UIDocument>(), Is.Empty);
        }

        private static IEnumerator WaitForScene(string sceneName, int maximumFrames)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (SceneManager.GetActiveScene().name == sceneName)
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.02f);
            }

            Assert.Fail($"Scene '{sceneName}' did not become active within {maximumFrames} frames.");
        }

        private static IEnumerator WaitForGeneration(ProceduralWorldSandbox sandbox, int maximumFrames)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (sandbox != null && sandbox.Fingerprint != 0UL && sandbox.RuntimeMetrics.QueueCount == 0)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Procedural sandbox generation did not complete within the expected frame budget.");
        }
    }
}
