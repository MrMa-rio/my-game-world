using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ProprioceptionSensorTests
    {
        [Test]
        public void PhysicsTick_LocomotionState_ProducesCompleteSnapshot()
        {
            GameObject root = new GameObject("Proprioception Test");
            GameObject schedulerRoot = new GameObject("Proprioception Scheduler");
            try
            {
                Actor actor = CreateActor(root);
                StubLocomotion locomotion = root.AddComponent<StubLocomotion>();
                actor.AddCapability<IActorLocomotion>(locomotion);
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                ProprioceptionSensor sensor = root.AddComponent<ProprioceptionSensor>();
                sensor.ConfigureScheduling(SensorTickMode.Physics, scheduler: scheduler);
                actor.AddSensor<IProprioceptionSensor>(sensor);
                locomotion.Current = CreateState(new Vector3(2f, 0f, 0f), true, LocomotionVerticalState.Grounded);

                scheduler.TickPhysics(0.02f);

                Assert.That(sensor.Current.Velocity, Is.EqualTo(new Vector3(2f, 0f, 0f)));
                Assert.That(sensor.Current.Acceleration, Is.EqualTo(Vector3.zero));
                Assert.That(sensor.Current.IsGrounded, Is.True);
                Assert.That(sensor.Current.MovementDirection, Is.EqualTo(Vector3.right));
                Assert.That(sensor.Current.MovementState, Is.EqualTo(ProprioceptiveMovementState.Moving));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); }
        }

        [Test]
        public void ConsecutiveTicks_ChangesVelocityAndRotation_DerivesRates()
        {
            GameObject root = new GameObject("Proprioception Rates Test");
            GameObject schedulerRoot = new GameObject("Proprioception Rates Scheduler");
            try
            {
                Actor actor = CreateActor(root);
                StubLocomotion locomotion = root.AddComponent<StubLocomotion>(); actor.AddCapability<IActorLocomotion>(locomotion);
                ActorSensorScheduler scheduler = schedulerRoot.AddComponent<ActorSensorScheduler>();
                ProprioceptionSensor sensor = root.AddComponent<ProprioceptionSensor>();
                sensor.ConfigureScheduling(SensorTickMode.Physics, scheduler: scheduler); actor.AddSensor<IProprioceptionSensor>(sensor);
                locomotion.Current = CreateState(Vector3.zero, true, LocomotionVerticalState.Grounded);
                scheduler.TickPhysics(0.5f);
                locomotion.Current = CreateState(new Vector3(0f, -2f, 0f), false, LocomotionVerticalState.Falling);
                root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                scheduler.TickPhysics(0.5f);

                Assert.That(sensor.Current.Acceleration.y, Is.EqualTo(-4f).Within(0.001f));
                Assert.That(sensor.Current.AngularVelocity.y, Is.EqualTo(Mathf.PI * 0.5f).Within(0.01f));
                Assert.That(sensor.Current.IsFalling, Is.True);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); }
        }

        private static LocomotionState CreateState(Vector3 velocity, bool grounded, LocomotionVerticalState vertical)
        {
            GroundProbeResult ground = grounded ? new GroundProbeResult(true, Vector3.up, 0f, null) : GroundProbeResult.Airborne;
            return new LocomotionState(velocity, ground, SlopeClassification.Walkable, CollisionFlags.None, vertical);
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1201), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private sealed class StubLocomotion : ActorCapability, IActorLocomotion
        {
            public LocomotionState Current { get; set; }
            public LocomotionState State => Current;
            public void SetDesiredMotion(Vector3 worldDirection, float speed) { }
            public void Simulate(float deltaTime) { }
            public bool TryAddVerticalImpulse(float speed) => false;
        }
    }
}
