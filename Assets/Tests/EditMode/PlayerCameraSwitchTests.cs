using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class PlayerCameraSwitchTests
    {
        [Test]
        public void ChangeCameraIntent_TogglesModesWithoutRecreatingRig()
        {
            GameObject actorRoot = new GameObject("Camera Switch Actor"); GameObject cameraRoot = new GameObject("Camera Switch Rig");
            PlayerCameraConfiguration config = ScriptableObject.CreateInstance<PlayerCameraConfiguration>();
            FirstPersonCameraProfile firstProfile = ScriptableObject.CreateInstance<FirstPersonCameraProfile>();
            ThirdPersonCameraProfile thirdProfile = ScriptableObject.CreateInstance<ThirdPersonCameraProfile>();
            try
            {
                Actor actor = CreateActor(actorRoot); PlayerCameraSystem system = cameraRoot.AddComponent<PlayerCameraSystem>();
                system.Initialize(actor, cameraRoot.AddComponent<Camera>(), config);
                system.Modes.Register(new FirstPersonCameraMode(firstProfile)); system.Modes.Register(new ThirdPersonCameraMode(thirdProfile));
                system.Modes.SetMode(PlayerCameraModeId.FirstPerson); PlayerCameraRig originalRig = system.Rig;
                PlayerCameraSwitchCapability switching = actorRoot.AddComponent<PlayerCameraSwitchCapability>();
                switching.Configure(system); actor.AddCapability<IPlayerCameraSwitchCapability>(switching);
                ChangeCameraIntent intent = new ChangeCameraIntent(1);
                actor.Intents.Submit(in intent);
                Assert.That(system.Modes.ActiveMode.Id, Is.EqualTo(PlayerCameraModeId.ThirdPerson));
                Assert.That(system.Rig, Is.SameAs(originalRig));
                actor.Intents.Submit(in intent);
                Assert.That(system.Modes.ActiveMode.Id, Is.EqualTo(PlayerCameraModeId.FirstPerson));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot); Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(config);
                Object.DestroyImmediate(firstProfile); Object.DestroyImmediate(thirdProfile);
            }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(2201), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
