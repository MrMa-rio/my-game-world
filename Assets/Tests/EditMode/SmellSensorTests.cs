using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class SmellSensorTests
    {
        [Test]
        public void IntervalSample_NearScent_DetectsThroughSharedField()
        {
            GameObject root = new GameObject("Smell Actor Test"); GameObject schedulerRoot = new GameObject("Smell Scheduler");
            SmellProfile profile = ScriptableObject.CreateInstance<SmellProfile>();
            try
            {
                Actor actor = CreateActor(root); ScentField field = new ScentField(); object key = new object();
                ScentSource source = new ScentSource(4, new Vector3(0f, 0f, 2f), 1f); field.Set(key, in source);
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                SmellSensor smell = root.AddComponent<SmellSensor>(); smell.Configure(profile, field);
                smell.ConfigureScheduling(SensorTickMode.Interval, 0.5f, scheduler); actor.AddSensor<ISmellSensor>(smell);
                scheduler.TickIntervals(0.5f);
                Assert.That(smell.Detected, Has.Count.EqualTo(1)); Assert.That(smell.Detected[0].Source.ScentId, Is.EqualTo(4));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); Object.DestroyImmediate(profile); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1601), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
