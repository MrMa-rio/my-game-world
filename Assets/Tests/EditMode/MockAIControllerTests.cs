using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class MockAIControllerTests
    {
        [Test]
        public void TickDecision_SameWalkRunAndLocomotion_MovesActorWithoutSystemChanges()
        {
            GameObject root = new GameObject("Mock AI Actor Test"); GameObject schedulerRoot = new GameObject("Mock AI Schedulers");
            LocomotionProfile locomotionProfile = ScriptableObject.CreateInstance<LocomotionProfile>();
            WalkProfile walkProfile = ScriptableObject.CreateInstance<WalkProfile>(); RunProfile runProfile = ScriptableObject.CreateInstance<RunProfile>();
            try
            {
                Actor actor = CreateActor(root); ActorLocomotion locomotion = root.AddComponent<ActorLocomotion>();
                locomotion.Configure(locomotionProfile, groundLayers: 0); actor.AddCapability<IActorLocomotion>(locomotion);
                WalkCapability walk = root.AddComponent<WalkCapability>(); walk.Configure(walkProfile); actor.AddCapability<IWalkCapability>(walk);
                RunCapability run = root.AddComponent<RunCapability>(); run.Configure(runProfile); actor.AddCapability<IRunCapability>(run);
                ActorDecisionScheduler decisions = schedulerRoot.AddComponent<ActorDecisionScheduler>();
                MockAIController ai = root.AddComponent<MockAIController>(); ai.Configure(decisions, 2.9f); actor.SetController(ai);
                decisions.Tick(0.2f); locomotion.Simulate(0.1f);
                Assert.That(root.transform.position.z, Is.GreaterThan(0f)); Assert.That(run.IsRunning, Is.True);
                Assert.That(walk.FinalSpeed, Is.EqualTo(walkProfile.Speed * runProfile.SpeedMultiplier).Within(0.001f));
                Assert.That(decisions.RegisteredCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(schedulerRoot); Object.DestroyImmediate(locomotionProfile);
                Object.DestroyImmediate(walkProfile); Object.DestroyImmediate(runProfile);
            }
        }
        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>(); entity.Initialize(new EntityId(3401), new GlobalPosition(),
                new WorldCoordinateFrame(new GlobalPosition()), new WorldEntityRegistry()); entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
