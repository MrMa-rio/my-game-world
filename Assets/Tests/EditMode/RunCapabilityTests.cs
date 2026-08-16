using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class RunCapabilityTests
    {
        [Test]
        public void Submit_RunIntent_AppliesAndRemovesOwnedSpeedModifier()
        {
            GameObject root = new GameObject("Run Modifier Test");
            LocomotionProfile locomotionProfile = ScriptableObject.CreateInstance<LocomotionProfile>();
            WalkProfile walkProfile = ScriptableObject.CreateInstance<WalkProfile>();
            RunProfile runProfile = ScriptableObject.CreateInstance<RunProfile>();
            try
            {
                Actor actor = CreateActor(root);
                AddLocomotion(actor, root, locomotionProfile);
                WalkCapability walk = AddWalk(actor, root, walkProfile);
                RunCapability run = root.AddComponent<RunCapability>();
                run.Configure(runProfile);
                actor.AddCapability<IRunCapability>(run);
                float walkingSpeed = walk.FinalSpeed;

                RunIntent start = new RunIntent(1, true);
                Assert.That(actor.Intents.Submit(in start), Is.EqualTo(IntentDispatchResult.Accepted));
                Assert.That(run.IsRunning, Is.True);
                Assert.That(walk.FinalSpeed, Is.EqualTo(walkingSpeed * runProfile.SpeedMultiplier).Within(0.001f));

                RunIntent stop = new RunIntent(2, false);
                actor.Intents.Submit(in stop);
                Assert.That(run.IsRunning, Is.False);
                Assert.That(walk.FinalSpeed, Is.EqualTo(walkingSpeed).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(locomotionProfile);
                Object.DestroyImmediate(walkProfile);
                Object.DestroyImmediate(runProfile);
            }
        }

        [Test]
        public void Disable_RunCapability_RestoresWalkingSpeed()
        {
            GameObject root = new GameObject("Run Disable Test");
            LocomotionProfile locomotionProfile = ScriptableObject.CreateInstance<LocomotionProfile>();
            WalkProfile walkProfile = ScriptableObject.CreateInstance<WalkProfile>();
            RunProfile runProfile = ScriptableObject.CreateInstance<RunProfile>();
            try
            {
                Actor actor = CreateActor(root);
                AddLocomotion(actor, root, locomotionProfile);
                WalkCapability walk = AddWalk(actor, root, walkProfile);
                RunCapability run = root.AddComponent<RunCapability>();
                run.Configure(runProfile);
                actor.AddCapability<IRunCapability>(run);
                float walkingSpeed = walk.FinalSpeed;
                RunIntent start = new RunIntent(1, true);
                actor.Intents.Submit(in start);

                run.SetEnabled(false);

                Assert.That(run.IsRunning, Is.False);
                Assert.That(walk.FinalSpeed, Is.EqualTo(walkingSpeed).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(locomotionProfile);
                Object.DestroyImmediate(walkProfile);
                Object.DestroyImmediate(runProfile);
            }
        }

        [Test]
        public void Resolve_MultipleSources_ComposesWithoutCategoryKnowledge()
        {
            MovementSpeedModifiers modifiers = new MovementSpeedModifiers();
            object run = new object();
            object terrain = new object();
            modifiers.Set(run, 1.8f);
            modifiers.Set(terrain, 0.5f);
            Assert.That(modifiers.Resolve(4f), Is.EqualTo(3.6f).Within(0.001f));
            modifiers.Remove(terrain);
            Assert.That(modifiers.Resolve(4f), Is.EqualTo(7.2f).Within(0.001f));
        }

        [Test]
        public void Resolve_AdditiveAndMultiplicativeSources_UsesStablePipeline()
        {
            MovementSpeedModifiers modifiers = new MovementSpeedModifiers(); object equipment = new object(); object buff = new object();
            MovementModifier equipmentModifier = new MovementModifier(0.5f, -1f, "Encumbrance");
            MovementModifier buffModifier = new MovementModifier(1.2f, 0f, "Buff");
            modifiers.Set(equipment, in equipmentModifier); modifiers.Set(buff, in buffModifier);
            ResolvedMovementSpeed result = modifiers.ResolveDetailed(6f);
            Assert.That(result.FinalSpeed, Is.EqualTo(3f).Within(0.001f));
            modifiers.Remove(equipment); Assert.That(modifiers.Resolve(6f), Is.EqualTo(7.2f).Within(0.001f));
        }

        private static void AddLocomotion(Actor actor, GameObject root, LocomotionProfile profile)
        {
            ActorLocomotion locomotion = root.AddComponent<ActorLocomotion>();
            locomotion.Configure(profile, groundLayers: 0);
            actor.AddCapability<IActorLocomotion>(locomotion);
        }

        private static WalkCapability AddWalk(Actor actor, GameObject root, WalkProfile profile)
        {
            WalkCapability walk = root.AddComponent<WalkCapability>();
            walk.Configure(profile);
            actor.AddCapability<IWalkCapability>(walk);
            return walk;
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(801), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
