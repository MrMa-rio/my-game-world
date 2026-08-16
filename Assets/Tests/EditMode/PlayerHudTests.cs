using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class PlayerHudTests
    {
        [Test]
        public void Presenter_CameraAndPromptChanges_PushesStateWithoutPolling()
        {
            GameObject actorRoot = new GameObject("HUD Actor Test"); GameObject cameraRoot = new GameObject("HUD Camera Test");
            PlayerCameraConfiguration config = ScriptableObject.CreateInstance<PlayerCameraConfiguration>();
            try
            {
                Actor actor = CreateActor(actorRoot); StubProprioception sensor = actorRoot.AddComponent<StubProprioception>();
                sensor.ConfigureScheduling(SensorTickMode.EventDriven); actor.AddSensor<IProprioceptionSensor>(sensor);
                PlayerCameraSystem cameraSystem = cameraRoot.AddComponent<PlayerCameraSystem>();
                cameraSystem.Initialize(actor, cameraRoot.AddComponent<Camera>(), config);
                StubCameraMode mode = new StubCameraMode(); cameraSystem.Modes.Register(mode);
                RecordingView view = new RecordingView();
                using (PlayerHudPresenter presenter = new PlayerHudPresenter(sensor, cameraSystem.Modes, view))
                {
                    presenter.SetInteractionPrompt("Interagir"); cameraSystem.Modes.SetMode(PlayerCameraModeId.FirstPerson);
                    Assert.That(view.State.InteractionPrompt, Is.EqualTo("Interagir"));
                    Assert.That(view.State.CameraMode, Is.EqualTo(PlayerCameraModeId.FirstPerson));
                    Assert.That(view.State.CrosshairVisible, Is.True);
                }
            }
            finally { Object.DestroyImmediate(actorRoot); Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(config); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(2401), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
        private sealed class RecordingView : IPlayerHudView { public PlayerHudState State { get; private set; } public void Render(in PlayerHudState state) => State = state; }
        private sealed class StubProprioception : ActorSensor, IProprioceptionSensor
        {
            public ProprioceptionSnapshot Current { get; private set; }
            public event System.Action<ProprioceptionSnapshot> Sampled;
            protected override void Sample() => Sampled?.Invoke(Current);
        }
        private sealed class StubCameraMode : IPlayerCameraMode
        {
            public PlayerCameraModeId Id => PlayerCameraModeId.FirstPerson;
            public void Enter(PlayerCameraRig rig, Actor actor) { } public void Exit() { } public void Tick(float deltaTime) { }
        }
    }
}
