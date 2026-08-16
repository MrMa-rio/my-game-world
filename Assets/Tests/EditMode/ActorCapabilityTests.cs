using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorCapabilityTests
    {
        [Test]
        public void AddCapability_InitializesAndQueriesByCapabilityContract()
        {
            GameObject root = new GameObject("Capability Test");
            try
            {
                Actor actor = CreateActor(root, active: true); TestCapability capability = root.AddComponent<TestCapability>();
                actor.AddCapability<ITestCapability>(capability);
                Assert.That(actor.Capabilities.TryGet(out ITestCapability resolved), Is.True);
                Assert.That(resolved, Is.SameAs(capability)); Assert.That(capability.Context, Is.SameAs(actor.Context));
                Assert.That(capability.CanExecute, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SetEnabled_ChangesCapabilityWithoutChangingActorOrRegistry()
        {
            GameObject root = new GameObject("Capability Toggle Test");
            try
            {
                Actor actor = CreateActor(root, active: true); TestCapability capability = root.AddComponent<TestCapability>();
                actor.AddCapability<ITestCapability>(capability);
                Assert.That(actor.Capabilities.SetEnabled<ITestCapability>(false), Is.True);
                Assert.That(capability.IsEnabled, Is.False); Assert.That(capability.CanExecute, Is.False);
                Assert.That(actor.Capabilities.Count, Is.EqualTo(1)); Assert.That(actor.State.CanAct, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CanExecute_ActorDisabled_BecomesFalseWithoutDisablingCapabilityConfiguration()
        {
            GameObject root = new GameObject("Capability Actor State Test");
            try
            {
                Actor actor = CreateActor(root, active: true); TestCapability capability = root.AddComponent<TestCapability>();
                actor.AddCapability<ITestCapability>(capability); actor.Entity.DisableEntity();
                Assert.That(capability.IsEnabled, Is.True); Assert.That(capability.CanExecute, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AddCapability_DuplicateContract_IsRejectedBeforeSecondInitialization()
        {
            GameObject root = new GameObject("Duplicate Capability Test");
            try
            {
                Actor actor = CreateActor(root, active: false);
                TestCapability first = root.AddComponent<TestCapability>(); TestCapability second = root.AddComponent<TestCapability>();
                actor.AddCapability<ITestCapability>(first);
                Assert.Throws<System.InvalidOperationException>(() => actor.AddCapability<ITestCapability>(second));
                Assert.That(second.IsInitialized, Is.False); Assert.That(actor.Capabilities.Count, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Actor CreateActor(GameObject root, bool active)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(301), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); if (active) entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private interface ITestCapability : IActorCapability { }
        private sealed class TestCapability : ActorCapability, ITestCapability { }
    }
}
