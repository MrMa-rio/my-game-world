using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorIntentTests
    {
        [Test]
        public void Submit_MoveIntent_RoutesDecisionToCapability()
        {
            GameObject root = new GameObject("Move Intent Test");
            try
            {
                Actor actor = CreateActor(root, true); MoveCapability capability = root.AddComponent<MoveCapability>();
                actor.AddCapability<IMoveCapability>(capability);
                MoveIntent intent = new MoveIntent(7, new Vector2(2f, 0f));
                Assert.That(actor.Intents.Submit(in intent), Is.EqualTo(IntentDispatchResult.Accepted));
                Assert.That(capability.LastSequence, Is.EqualTo(7));
                Assert.That(capability.LastDirection, Is.EqualTo(Vector2.right));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Submit_DisabledCapability_DoesNotExecuteHandler()
        {
            GameObject root = new GameObject("Disabled Intent Handler Test");
            try
            {
                Actor actor = CreateActor(root, true); MoveCapability capability = root.AddComponent<MoveCapability>();
                actor.AddCapability<IMoveCapability>(capability); capability.SetEnabled(false);
                MoveIntent intent = new MoveIntent(1, Vector2.up);
                Assert.That(actor.Intents.Submit(in intent), Is.EqualTo(IntentDispatchResult.HandlerUnavailable));
                Assert.That(capability.HandleCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Submit_InactiveActor_DoesNotExecuteHandler()
        {
            GameObject root = new GameObject("Inactive Actor Intent Test");
            try
            {
                Actor actor = CreateActor(root, false); MoveCapability capability = root.AddComponent<MoveCapability>();
                actor.AddCapability<IMoveCapability>(capability); MoveIntent intent = new MoveIntent(1, Vector2.up);
                Assert.That(actor.Intents.Submit(in intent), Is.EqualTo(IntentDispatchResult.ActorUnavailable));
                Assert.That(capability.HandleCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RemoveCapability_UnregistersItsIntentRoutes()
        {
            GameObject root = new GameObject("Remove Intent Route Test");
            try
            {
                Actor actor = CreateActor(root, true); MoveCapability capability = root.AddComponent<MoveCapability>();
                actor.AddCapability<IMoveCapability>(capability); Assert.That(actor.RemoveCapability<IMoveCapability>(capability), Is.True);
                MoveIntent intent = new MoveIntent(1, Vector2.zero);
                Assert.That(actor.Intents.Submit(in intent), Is.EqualTo(IntentDispatchResult.NoHandler));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void IntentTypes_CarryControllerDecisionWithoutExecutionDependencies()
        {
            Assert.That(new RunIntent(2, true).Requested, Is.True);
            Assert.That(new LookIntent(3, new Vector2(4f, -2f)).Delta, Is.EqualTo(new Vector2(4f, -2f)));
            Assert.That(new JumpIntent(4).Sequence, Is.EqualTo(4));
            Assert.That(new InteractIntent(5).Sequence, Is.EqualTo(5));
        }

        private static Actor CreateActor(GameObject root, bool active)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(401), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); if (active) entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private interface IMoveCapability : IActorCapability { }
        private sealed class MoveCapability : ActorCapability, IMoveCapability, IActorIntentHandler<MoveIntent>
        {
            public int HandleCount { get; private set; }
            public ulong LastSequence { get; private set; }
            public Vector2 LastDirection { get; private set; }
            protected override void OnInitialized() => RegisterIntentHandler<MoveIntent>(this);
            public void HandleIntent(in MoveIntent intent)
            { HandleCount++; LastSequence = intent.Sequence; LastDirection = intent.Direction; }
        }
    }
}
