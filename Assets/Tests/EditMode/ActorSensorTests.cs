using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorSensorTests
    {
        [Test]
        public void TickIntervals_RespectsConfiguredFrequencyAndCentralRegistration()
        {
            GameObject root = new GameObject("Sensor Actor Test");
            GameObject schedulerRoot = new GameObject("Sensor Scheduler Test");
            try
            {
                Actor actor = CreateActor(root);
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                CountingSensor sensor = root.AddComponent<CountingSensor>();
                sensor.ConfigureScheduling(SensorTickMode.Interval, 0.2f, scheduler);
                actor.AddSensor<ICountingSensor>(sensor);
                Assert.That(scheduler.RegisteredCount, Is.EqualTo(1));

                scheduler.TickIntervals(0.1f);
                Assert.That(sensor.SampleCount, Is.Zero);
                scheduler.TickIntervals(0.1f);
                Assert.That(sensor.SampleCount, Is.EqualTo(1));

                actor.RemoveSensor<ICountingSensor>(sensor);
                Assert.That(scheduler.RegisteredCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); }
        }

        [Test]
        public void PhysicsSensor_SamplesOnlyOnPhysicsTicks()
        {
            GameObject root = new GameObject("Physics Sensor Test");
            GameObject schedulerRoot = new GameObject("Physics Sensor Scheduler Test");
            try
            {
                Actor actor = CreateActor(root);
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                CountingSensor sensor = root.AddComponent<CountingSensor>();
                sensor.ConfigureScheduling(SensorTickMode.Physics, scheduler: scheduler);
                actor.AddSensor<ICountingSensor>(sensor);
                scheduler.TickIntervals(1f);
                Assert.That(sensor.SampleCount, Is.Zero);
                scheduler.TickPhysics(0.02f);
                Assert.That(sensor.SampleCount, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1101), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private interface ICountingSensor : IActorSensor { }
        private sealed class CountingSensor : ActorSensor, ICountingSensor
        {
            public int SampleCount { get; private set; }
            protected override void Sample() => SampleCount++;
        }
    }
}
