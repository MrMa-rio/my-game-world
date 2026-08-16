using System;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorAnimationDriverTests
    {
        [Test]
        public void ProprioceptionSamples_DeriveMovementStatesWithoutAnimatorGameplay()
        {
            GameObject root = new GameObject("Animation Driver Test");
            ActorAnimationDriverProfile profile = ScriptableObject.CreateInstance<ActorAnimationDriverProfile>();
            try
            {
                Actor actor = CreateActor(root); StubProprioception sensor = root.AddComponent<StubProprioception>();
                actor.AddSensor<IProprioceptionSensor>(sensor); RecordingSink sink = new RecordingSink();
                ActorAnimationDriver driver = root.AddComponent<ActorAnimationDriver>(); driver.Initialize(actor, profile, sink);
                sensor.Raise(CreateSnapshot(new Vector3(0f, 0f, 2f), ProprioceptiveMovementState.Moving, true));
                Assert.That(sink.Last.Movement, Is.EqualTo(ActorAnimationMovementState.Walk));
                sensor.Raise(CreateSnapshot(new Vector3(0f, 0f, 8f), ProprioceptiveMovementState.Moving, true));
                Assert.That(sink.Last.Movement, Is.EqualTo(ActorAnimationMovementState.Run));
                sensor.Raise(CreateSnapshot(new Vector3(0f, -3f, 0f), ProprioceptiveMovementState.Falling, false));
                Assert.That(sink.Last.Movement, Is.EqualTo(ActorAnimationMovementState.Fall));
                Assert.That(sink.ApplyCount, Is.EqualTo(3));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(profile); }
        }

        private static ProprioceptionSnapshot CreateSnapshot(Vector3 velocity, ProprioceptiveMovementState state, bool grounded)
            => new ProprioceptionSnapshot(velocity, Vector3.zero, Quaternion.identity, Vector3.zero, grounded, 0f,
                new Vector3(velocity.x, 0f, velocity.z).normalized, state);
        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>(); entity.Initialize(new EntityId(3001), new GlobalPosition(),
                new WorldCoordinateFrame(new GlobalPosition()), new WorldEntityRegistry()); entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
        private sealed class StubProprioception : ActorSensor, IProprioceptionSensor
        {
            public ProprioceptionSnapshot Current { get; private set; }
            public event Action<ProprioceptionSnapshot> Sampled;
            public void Raise(ProprioceptionSnapshot snapshot) { Current = snapshot; Sampled?.Invoke(snapshot); }
            protected override void Sample() { }
        }
        private sealed class RecordingSink : IActorAnimationSink
        {
            public ActorAnimationState Last { get; private set; } public int ApplyCount { get; private set; }
            public void Apply(in ActorAnimationState state) { Last = state; ApplyCount++; }
        }
    }
}
