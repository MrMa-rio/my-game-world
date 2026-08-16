using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class JumpCapabilityTests
    {
        [Test]
        public void Submit_JumpIntent_GroundedLocomotionReceivesImpulse()
        {
            GameObject root = new GameObject("Jump Intent Test");
            JumpProfile profile = ScriptableObject.CreateInstance<JumpProfile>();
            try
            {
                Actor actor = CreateActor(root);
                StubLocomotion locomotion = root.AddComponent<StubLocomotion>();
                locomotion.Grounded = true;
                actor.AddCapability<IActorLocomotion>(locomotion);
                JumpCapability jump = root.AddComponent<JumpCapability>();
                jump.Configure(profile);
                actor.AddCapability<IJumpCapability>(jump);
                JumpIntent intent = new JumpIntent(1);

                Assert.That(actor.Intents.Submit(in intent), Is.EqualTo(IntentDispatchResult.Accepted));
                Assert.That(locomotion.ImpulseCount, Is.EqualTo(1));
                Assert.That(locomotion.LastImpulse, Is.EqualTo(profile.VerticalSpeed));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Submit_JumpIntent_AirborneLocomotionRejectsImpulse()
        {
            GameObject root = new GameObject("Airborne Jump Test");
            JumpProfile profile = ScriptableObject.CreateInstance<JumpProfile>();
            try
            {
                Actor actor = CreateActor(root);
                StubLocomotion locomotion = root.AddComponent<StubLocomotion>();
                actor.AddCapability<IActorLocomotion>(locomotion);
                JumpCapability jump = root.AddComponent<JumpCapability>();
                jump.Configure(profile); actor.AddCapability<IJumpCapability>(jump);
                JumpIntent intent = new JumpIntent(1);

                actor.Intents.Submit(in intent);

                Assert.That(locomotion.ImpulseCount, Is.Zero);
                Assert.That(jump.NextAllowedTime, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(901), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private sealed class StubLocomotion : ActorCapability, IActorLocomotion
        {
            public bool Grounded { get; set; }
            public int ImpulseCount { get; private set; }
            public float LastImpulse { get; private set; }
            public LocomotionState State => new LocomotionState(Vector3.zero,
                Grounded ? new GroundProbeResult(true, Vector3.up, 0f, null) : GroundProbeResult.Airborne,
                SlopeClassification.Walkable, CollisionFlags.None,
                Grounded ? LocomotionVerticalState.Grounded : LocomotionVerticalState.Falling);
            public void SetDesiredMotion(Vector3 worldDirection, float speed) { }
            public void Simulate(float deltaTime) { }
            public bool TryAddVerticalImpulse(float speed)
            {
                if (!Grounded) return false;
                ImpulseCount++; LastImpulse = speed; Grounded = false; return true;
            }
        }
    }
}
