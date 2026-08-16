using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class FirstPersonCameraModeTests
    {
        [Test]
        public void LookIntent_FirstPersonMode_RotatesActorAndClampsPitch()
        {
            GameObject actorRoot = new GameObject("First Person Actor Test");
            GameObject cameraRoot = new GameObject("First Person Camera Test");
            PlayerCameraConfiguration configuration = ScriptableObject.CreateInstance<PlayerCameraConfiguration>();
            FirstPersonCameraProfile profile = ScriptableObject.CreateInstance<FirstPersonCameraProfile>();
            try
            {
                Actor actor = CreateActor(actorRoot); Camera camera = cameraRoot.AddComponent<Camera>();
                PlayerCameraSystem system = cameraRoot.AddComponent<PlayerCameraSystem>(); system.Initialize(actor, camera, configuration);
                FirstPersonCameraMode mode = new FirstPersonCameraMode(profile); system.Modes.Register(mode); system.Modes.SetMode(PlayerCameraModeId.FirstPerson);
                PlayerCameraLookBridge bridge = actorRoot.AddComponent<PlayerCameraLookBridge>(); bridge.Configure(system);
                actor.AddCapability<IPlayerCameraLookBridge>(bridge);
                LookIntent look = new LookIntent(1, new Vector2(100f, 10000f));

                Assert.That(actor.Intents.Submit(in look), Is.EqualTo(IntentDispatchResult.Accepted));
                system.Modes.Tick(0.016f);

                Assert.That(actorRoot.transform.eulerAngles.y, Is.GreaterThan(0f));
                Assert.That(mode.Pitch, Is.EqualTo(profile.MinimumPitch).Within(0.001f));
                Assert.That(cameraRoot.transform.position.y, Is.EqualTo(profile.EyeHeight).Within(0.001f));
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
            entity.Initialize(new EntityId(2001), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
