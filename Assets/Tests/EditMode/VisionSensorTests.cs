using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class VisionSensorTests
    {
        [Test]
        public void IntervalSample_TargetAhead_DetectsWithoutCamera()
        {
            GameObject actorRoot = new GameObject("Vision Actor Test");
            GameObject targetRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject schedulerRoot = new GameObject("Vision Scheduler Test");
            VisionProfile profile = ScriptableObject.CreateInstance<VisionProfile>();
            try
            {
                Actor actor = CreateActor(actorRoot);
                targetRoot.transform.position = new Vector3(0f, 1.65f, 5f);
                VisionTarget target = targetRoot.AddComponent<VisionTarget>();
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                VisionSensor vision = actorRoot.AddComponent<VisionSensor>();
                vision.Configure(profile); vision.ConfigureScheduling(SensorTickMode.Interval, 0.1f, scheduler);
                actor.AddSensor<IVisionSensor>(vision);
                Physics.SyncTransforms();

                scheduler.TickIntervals(0.1f);

                Assert.That(vision.Detected, Has.Count.EqualTo(1));
                Assert.That(vision.Detected[0], Is.SameAs(target));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot); Object.DestroyImmediate(targetRoot);
                Object.DestroyImmediate(schedulerRoot); Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void IntervalSample_TargetBehind_IsOutsideFieldOfView()
        {
            GameObject actorRoot = new GameObject("Vision FOV Actor Test");
            GameObject targetRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject schedulerRoot = new GameObject("Vision FOV Scheduler Test");
            VisionProfile profile = ScriptableObject.CreateInstance<VisionProfile>();
            try
            {
                Actor actor = CreateActor(actorRoot);
                targetRoot.transform.position = new Vector3(0f, 1.65f, -5f); targetRoot.AddComponent<VisionTarget>();
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                VisionSensor vision = actorRoot.AddComponent<VisionSensor>();
                vision.Configure(profile); vision.ConfigureScheduling(SensorTickMode.Interval, 0.1f, scheduler);
                actor.AddSensor<IVisionSensor>(vision); Physics.SyncTransforms(); scheduler.TickIntervals(0.1f);
                Assert.That(vision.Detected, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(actorRoot); Object.DestroyImmediate(targetRoot);
                Object.DestroyImmediate(schedulerRoot); Object.DestroyImmediate(profile);
            }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1401), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
