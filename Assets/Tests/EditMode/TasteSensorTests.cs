using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class TasteSensorTests
    {
        [Test]
        public void Sense_ExplicitStimulus_PublishesWithoutScheduler()
        {
            GameObject root = new GameObject("Taste Actor Test");
            try
            {
                Actor actor = CreateActor(root); TasteSensor taste = root.AddComponent<TasteSensor>();
                taste.ConfigureScheduling(SensorTickMode.EventDriven); actor.AddSensor<ITasteSensor>(taste);
                int count = 0; taste.TasteSensed += _ => count++;
                TasteStimulus stimulus = new TasteStimulus(9, 0.8f, 0.1f, 5f);
                Assert.That(taste.Sense(in stimulus), Is.True);
                Assert.That(count, Is.EqualTo(1)); Assert.That(taste.LastTaste.FlavorId, Is.EqualTo(9));
                Assert.That(taste.LastTaste.Nutrition, Is.EqualTo(5f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1701), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
