using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ThirdPersonCameraModeTests
    {
        [Test]
        public void EnterAndLook_ThirdPersonMode_FollowsAndOrbitsActor()
        {
            GameObject actorRoot = new GameObject("Third Person Actor Test"); GameObject cameraRoot = new GameObject("Third Person Camera Test");
            PlayerCameraConfiguration configuration = ScriptableObject.CreateInstance<PlayerCameraConfiguration>();
            ThirdPersonCameraProfile profile = ScriptableObject.CreateInstance<ThirdPersonCameraProfile>();
            try
            {
                Actor actor = CreateActor(actorRoot); Camera camera = cameraRoot.AddComponent<Camera>();
                PlayerCameraSystem system = cameraRoot.AddComponent<PlayerCameraSystem>(); system.Initialize(actor, camera, configuration);
                ThirdPersonCameraMode mode = new ThirdPersonCameraMode(profile); system.Modes.Register(mode);
                Assert.That(system.Modes.SetMode(PlayerCameraModeId.ThirdPerson), Is.True);
                Vector3 initialPosition = cameraRoot.transform.position;
                Assert.That(Vector3.Distance(initialPosition, Vector3.up * profile.PivotHeight), Is.GreaterThan(4f));

                system.SubmitLook(new Vector2(100f, 20f)); system.Modes.Tick(0.5f);

                Assert.That(mode.Yaw, Is.GreaterThan(0f));
                Assert.That(cameraRoot.transform.position, Is.Not.EqualTo(initialPosition));
                Assert.That(mode.Pitch, Is.LessThan(0f));
                Assert.That(Quaternion.Angle(cameraRoot.transform.rotation,
                    Quaternion.Euler(mode.Pitch, mode.Yaw, 0f)), Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot); Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(configuration); Object.DestroyImmediate(profile);
            }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(2101), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
