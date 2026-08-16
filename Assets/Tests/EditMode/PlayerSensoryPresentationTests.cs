using System;
using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class PlayerSensoryPresentationTests
    {
        [Test]
        public void SensorEvents_AreForwardedToPlayerFeedback()
        {
            GameObject root = new GameObject("Sensory Presentation Test");
            try
            {
                Actor actor = CreateActor(root); AddSensors(actor, root); RecordingFeedback feedback = new RecordingFeedback();
                using (new PlayerSensoryPresentation(actor, feedback))
                {
                    ITasteSensor taste; actor.Sensors.TryGet(out taste); TasteStimulus stimulus = new TasteStimulus(42, 1f, 0f, 2f);
                    taste.Sense(in stimulus);
                    Assert.That(feedback.TasteCount, Is.EqualTo(1)); Assert.That(feedback.LastTaste.FlavorId, Is.EqualTo(42));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void AddSensors(Actor actor, GameObject root)
        {
            actor.AddSensor<IVisionSensor>(root.AddComponent<StubVision>());
            actor.AddSensor<IHearingSensor>(root.AddComponent<StubHearing>());
            actor.AddSensor<ITouchSensor>(root.AddComponent<StubTouch>());
            actor.AddSensor<ISmellSensor>(root.AddComponent<StubSmell>());
            TasteSensor taste = root.AddComponent<TasteSensor>(); taste.ConfigureScheduling(SensorTickMode.EventDriven); actor.AddSensor<ITasteSensor>(taste);
            actor.AddSensor<IProprioceptionSensor>(root.AddComponent<StubProprioception>());
        }
        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>(); entity.Initialize(new EntityId(2501), new GlobalPosition(),
                new WorldCoordinateFrame(new GlobalPosition()), new WorldEntityRegistry()); entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
        private abstract class StubSensor : ActorSensor { protected override void Sample() { } }
        private sealed class StubVision : StubSensor, IVisionSensor { public IReadOnlyList<IVisionTarget> Detected => Array.Empty<IVisionTarget>(); public event Action<IReadOnlyList<IVisionTarget>> PerceptionUpdated; }
        private sealed class StubHearing : StubSensor, IHearingSensor { public event Action<HeardSound> SoundHeard; }
        private sealed class StubTouch : StubSensor, ITouchSensor { public TouchEvent LastContact => default; public event Action<TouchEvent> ContactSensed; }
        private sealed class StubSmell : StubSensor, ISmellSensor { public IReadOnlyList<DetectedScent> Detected => Array.Empty<DetectedScent>(); public event Action<IReadOnlyList<DetectedScent>> PerceptionUpdated; }
        private sealed class StubProprioception : StubSensor, IProprioceptionSensor { public ProprioceptionSnapshot Current => default; public event Action<ProprioceptionSnapshot> Sampled; }
        private sealed class RecordingFeedback : IPlayerSensoryFeedback
        {
            public int TasteCount { get; private set; } public TasteStimulus LastTaste { get; private set; }
            public void PresentTaste(TasteStimulus taste) { TasteCount++; LastTaste = taste; }
            public void PresentVision(IReadOnlyList<IVisionTarget> targets) { } public void PresentHearing(HeardSound sound) { }
            public void PresentTouch(TouchEvent contact) { } public void PresentSmell(IReadOnlyList<DetectedScent> scents) { }
            public void PresentProprioception(ProprioceptionSnapshot state) { }
        }
    }
}
