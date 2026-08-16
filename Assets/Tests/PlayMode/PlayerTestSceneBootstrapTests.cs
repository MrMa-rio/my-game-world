using System.Collections;
using MyGameWorld.Client.PlayerRuntime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MyGameWorld.Tests.PlayMode
{
    public sealed class PlayerTestSceneBootstrapTests
    {
        [UnityTest]
        public IEnumerator Awake_InputConfigured_AssemblesPlayableTechnicalPlayer()
        {
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>(); InputActionMap map = actions.AddActionMap("Player");
            map.AddAction("Move", InputActionType.Value); map.AddAction("Look", InputActionType.Value);
            map.AddAction("Sprint", InputActionType.Button); map.AddAction("Jump", InputActionType.Button); map.AddAction("Interact", InputActionType.Button);
            GameObject root = new GameObject("Player Bootstrap PlayMode Test"); root.SetActive(false);
            PlayerTestSceneBootstrap bootstrap = root.AddComponent<PlayerTestSceneBootstrap>(); bootstrap.SetInputActions(actions); root.SetActive(true);
            yield return null;
            Assert.That(bootstrap.PlayerActor, Is.Not.Null); Assert.That(bootstrap.PlayerActor.State.CanAct, Is.True);
            Assert.That(bootstrap.CameraSystem, Is.Not.Null); Assert.That(bootstrap.CameraSystem.Modes.ActiveMode.Id, Is.EqualTo(PlayerCameraModeId.ThirdPerson));
            Object.Destroy(root); Object.Destroy(actions); yield return null;
        }
    }
}
