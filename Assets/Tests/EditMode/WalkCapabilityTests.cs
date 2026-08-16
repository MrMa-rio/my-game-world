using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class WalkCapabilityTests
    {
        [Test]
        public void AddCapability_WithoutLocomotion_IsRejectedAndRolledBack()
        {
            GameObject root = new GameObject("Walk Dependency Test");
            WalkProfile walkProfile = ScriptableObject.CreateInstance<WalkProfile>();
            try
            {
                Actor actor = CreateActor(root);
                WalkCapability walk = root.AddComponent<WalkCapability>();
                walk.Configure(walkProfile);

                Assert.Throws<System.InvalidOperationException>(() => actor.AddCapability<IWalkCapability>(walk));
                Assert.That(walk.IsInitialized, Is.False);
                Assert.That(actor.Intents.HandlerCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(walkProfile);
            }
        }

        [Test]
        public void Submit_MoveIntent_WalksRelativeToActorOrientation()
        {
            GameObject root = new GameObject("Walk Direction Test");
            LocomotionProfile locomotionProfile = ScriptableObject.CreateInstance<LocomotionProfile>();
            WalkProfile walkProfile = ScriptableObject.CreateInstance<WalkProfile>();
            try
            {
                Actor actor = CreateActor(root);
                root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                ActorLocomotion locomotion = AddLocomotion(actor, root, locomotionProfile);
                AddWalk(actor, root, walkProfile);
                MoveIntent intent = new MoveIntent(1, Vector2.up);

                Assert.That(actor.Intents.Submit(in intent), Is.EqualTo(IntentDispatchResult.Accepted));
                locomotion.Simulate(0.1f);

                Assert.That(root.transform.position.x, Is.GreaterThan(0f));
                Assert.That(Mathf.Abs(root.transform.position.z), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(locomotionProfile);
                Object.DestroyImmediate(walkProfile);
            }
        }

        [Test]
        public void Submit_ZeroMoveIntent_DeceleratesUntilStopped()
        {
            GameObject root = new GameObject("Walk Stop Test");
            LocomotionProfile locomotionProfile = ScriptableObject.CreateInstance<LocomotionProfile>();
            WalkProfile walkProfile = ScriptableObject.CreateInstance<WalkProfile>();
            try
            {
                Actor actor = CreateActor(root);
                ActorLocomotion locomotion = AddLocomotion(actor, root, locomotionProfile);
                AddWalk(actor, root, walkProfile);
                MoveIntent moving = new MoveIntent(1, Vector2.up);
                actor.Intents.Submit(in moving);
                locomotion.Simulate(0.1f);
                Assert.That(locomotion.State.Velocity.z, Is.GreaterThan(0f));

                MoveIntent stopped = new MoveIntent(2, Vector2.zero);
                actor.Intents.Submit(in stopped);
                locomotion.Simulate(0.1f);

                Assert.That(locomotion.State.Velocity.z, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(locomotionProfile);
                Object.DestroyImmediate(walkProfile);
            }
        }

        private static ActorLocomotion AddLocomotion(Actor actor, GameObject root, LocomotionProfile profile)
        {
            ActorLocomotion locomotion = root.AddComponent<ActorLocomotion>();
            locomotion.Configure(profile, groundLayers: 0);
            actor.AddCapability<IActorLocomotion>(locomotion);
            return locomotion;
        }

        private static void AddWalk(Actor actor, GameObject root, WalkProfile profile)
        {
            WalkCapability walk = root.AddComponent<WalkCapability>();
            walk.Configure(profile);
            actor.AddCapability<IWalkCapability>(walk);
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(701), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn();
            entity.Activate();
            Actor actor = root.AddComponent<Actor>();
            actor.Initialize(entity);
            return actor;
        }
    }
}
