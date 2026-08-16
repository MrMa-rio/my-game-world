using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorFoundationTests
    {
        [Test]
        public void Initialize_ActiveWorldEntity_ExposesContextAndTracksAvailability()
        {
            GameObject root = new GameObject("Actor Foundation Test");
            try
            {
                WorldEntity entity = CreateEntity(root, 101); entity.Spawn(); entity.Activate();
                Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity);
                Assert.That(actor.Context.Entity, Is.SameAs(entity));
                Assert.That(actor.Context.Presence, Is.SameAs(entity.Presence));
                Assert.That(actor.State.Availability, Is.EqualTo(ActorAvailability.Active));
                entity.DisableEntity();
                Assert.That(actor.State.Availability, Is.EqualTo(ActorAvailability.Disabled));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CompositionRegistries_QueryByContractWithoutComponentSearch()
        {
            GameObject root = new GameObject("Actor Sensor Registry Test");
            try
            {
                WorldEntity entity = CreateEntity(root, 103); entity.Spawn(); entity.Activate();
                Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity);
                TestSensor sensor = root.AddComponent<TestSensor>(); actor.AddSensor<ITestSensor>(sensor);
                Assert.That(actor.Sensors.TryGet(out ITestSensor resolvedSensor), Is.True);
                Assert.That(resolvedSensor, Is.SameAs(sensor));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SetController_ReplacesDecisionSourceWithoutChangingActorComposition()
        {
            GameObject root = new GameObject("Actor Controller Test");
            try
            {
                WorldEntity entity = CreateEntity(root, 102); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity);
                TestController first = new TestController(); TestController second = new TestController();
                actor.SetController(first); actor.SetController(second);
                Assert.That(first.IsBound, Is.False); Assert.That(second.IsBound, Is.True);
                Assert.That(second.Context, Is.SameAs(actor.Context)); Assert.That(actor.Controller, Is.SameAs(second));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Initialize_WithoutInitializedWorldEntity_IsRejected()
        {
            GameObject root = new GameObject("Invalid Actor Test");
            try { Actor actor = root.AddComponent<Actor>(); Assert.Throws<System.InvalidOperationException>(() => actor.Initialize()); }
            finally { Object.DestroyImmediate(root); }
        }

        private static WorldEntity CreateEntity(GameObject root, long id)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(id), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            return entity;
        }

        private interface ITestSensor : IActorSensor { }
        private sealed class TestSensor : ActorSensor, ITestSensor
        {
            protected override void Sample() { }
        }
        private sealed class TestController : IActorController
        {
            public bool IsBound { get; private set; }
            public ActorContext Context { get; private set; }
            public void Bind(ActorContext context) { Context = context; IsBound = true; }
            public void Unbind() { Context = null; IsBound = false; }
        }
    }
}
