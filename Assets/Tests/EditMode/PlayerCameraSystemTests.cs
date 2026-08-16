using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class PlayerCameraSystemTests
    {
        [Test]
        public void SetMode_RegisteredStrategies_ExitsPreviousAndEntersNext()
        {
            GameObject actorRoot = new GameObject("Camera Actor Test"); GameObject cameraRoot = new GameObject("Camera Rig Test");
            PlayerCameraConfiguration configuration = ScriptableObject.CreateInstance<PlayerCameraConfiguration>();
            try
            {
                Actor actor = CreateActor(actorRoot); Camera camera = cameraRoot.AddComponent<Camera>();
                PlayerCameraSystem system = cameraRoot.AddComponent<PlayerCameraSystem>(); system.Initialize(actor, camera, configuration);
                StubMode first = new StubMode(PlayerCameraModeId.FirstPerson); StubMode third = new StubMode(PlayerCameraModeId.ThirdPerson);
                system.Modes.Register(first); system.Modes.Register(third);
                Assert.That(system.Modes.SetMode(PlayerCameraModeId.FirstPerson), Is.True);
                Assert.That(system.Modes.SetMode(PlayerCameraModeId.ThirdPerson), Is.True);
                Assert.That(first.EnterCount, Is.EqualTo(1)); Assert.That(first.ExitCount, Is.EqualTo(1));
                Assert.That(third.EnterCount, Is.EqualTo(1)); Assert.That(system.Modes.ActiveMode, Is.SameAs(third));
            }
            finally { Object.DestroyImmediate(actorRoot); Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(configuration); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1901), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private sealed class StubMode : IPlayerCameraMode
        {
            public StubMode(PlayerCameraModeId id) => Id = id;
            public PlayerCameraModeId Id { get; }
            public int EnterCount { get; private set; } public int ExitCount { get; private set; }
            public void Enter(PlayerCameraRig rig, Actor actor) => EnterCount++;
            public void Exit() => ExitCount++;
            public void Tick(float deltaTime) { }
        }
    }
}
