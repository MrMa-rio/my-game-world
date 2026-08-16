using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class EnvironmentContextSensorTests
    {
        [Test]
        public void IntervalSample_Provider_UpdatesActorEnvironmentContext()
        {
            GameObject root = new GameObject("Environment Actor Test"); GameObject schedulerRoot = new GameObject("Environment Scheduler");
            try
            {
                Actor actor = CreateActor(root); ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                EnvironmentContextSensor sensor = root.AddComponent<EnvironmentContextSensor>(); sensor.Configure(new StubProvider());
                sensor.ConfigureScheduling(SensorTickMode.Interval, 0.5f, scheduler); actor.AddSensor<IEnvironmentContextSensor>(sensor);
                scheduler.TickIntervals(0.5f);
                Assert.That(sensor.Current.BiomeId, Is.EqualTo(3)); Assert.That(sensor.Current.SurfaceId, Is.EqualTo(5));
                Assert.That(sensor.Current.WindStrength, Is.EqualTo(0.75f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); }
        }
        private sealed class StubProvider : IWorldEnvironmentContextProvider
        { public WorldEnvironmentSnapshot Sample(Vector3 local, GlobalPosition global) => new WorldEnvironmentSnapshot(3, 5, Vector3.right, 0.75f, 0); }
        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>(); entity.Initialize(new EntityId(2701), new GlobalPosition(),
                new WorldCoordinateFrame(new GlobalPosition()), new WorldEntityRegistry()); entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
