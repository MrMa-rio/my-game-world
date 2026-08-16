using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorLocomotionTests
    {
        [Test]
        public void Classify_DefaultProfile_ProducesFourSlopeBands()
        {
            LocomotionProfile profile = ScriptableObject.CreateInstance<LocomotionProfile>();
            try
            {
                SlopeResolver resolver = new SlopeResolver(profile);
                Assert.That(resolver.Classify(30f), Is.EqualTo(SlopeClassification.Walkable));
                Assert.That(resolver.Classify(47f), Is.EqualTo(SlopeClassification.Difficult));
                Assert.That(resolver.Classify(57f), Is.EqualTo(SlopeClassification.Slide));
                Assert.That(resolver.Classify(70f), Is.EqualTo(SlopeClassification.Blocked));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Step_Airborne_ClampsGravityAtTerminalSpeed()
        {
            LocomotionProfile profile = ScriptableObject.CreateInstance<LocomotionProfile>();
            try
            {
                GravityResolver gravity = new GravityResolver(profile);
                for (int index = 0; index < 1000; index++) gravity.Step(false, CollisionFlags.None, 0.02f);
                Assert.That(gravity.VerticalVelocity, Is.EqualTo(-profile.TerminalFallSpeed).Within(0.001f));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Simulate_DesiredMotion_MovesActorAndSynchronizesWorldPresence()
        {
            GameObject root = new GameObject("Locomotion Movement Test");
            LocomotionProfile profile = ScriptableObject.CreateInstance<LocomotionProfile>();
            try
            {
                Actor actor = CreateActor(root);
                ActorLocomotion locomotion = root.AddComponent<ActorLocomotion>();
                locomotion.Configure(profile, groundLayers: 0);
                actor.AddCapability<IActorLocomotion>(locomotion);
                locomotion.SetDesiredMotion(Vector3.forward, 4f);

                locomotion.Simulate(0.1f);

                Assert.That(root.transform.position.z, Is.GreaterThan(0f));
                Assert.That(actor.Context.Presence.LocalPosition, Is.EqualTo(root.transform.position));
                Assert.That(locomotion.State.Velocity.z, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void RemoveCapability_UnregistersLocomotionFromCentralScheduler()
        {
            GameObject root = new GameObject("Locomotion Registration Test");
            GameObject schedulerRoot = new GameObject("Locomotion Scheduler Test");
            LocomotionProfile profile = ScriptableObject.CreateInstance<LocomotionProfile>();
            try
            {
                Actor actor = CreateActor(root);
                ActorLocomotionScheduler scheduler = schedulerRoot.AddComponent<ActorLocomotionScheduler>();
                ActorLocomotion locomotion = root.AddComponent<ActorLocomotion>();
                locomotion.Configure(profile, scheduler);
                actor.AddCapability<IActorLocomotion>(locomotion);
                Assert.That(scheduler.RegisteredCount, Is.EqualTo(1));

                Assert.That(actor.RemoveCapability<IActorLocomotion>(locomotion), Is.True);

                Assert.That(scheduler.RegisteredCount, Is.Zero);
                Assert.That(locomotion.IsInitialized, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(schedulerRoot);
                Object.DestroyImmediate(profile);
            }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(601), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn();
            entity.Activate();
            Actor actor = root.AddComponent<Actor>();
            actor.Initialize(entity);
            return actor;
        }
    }
}
