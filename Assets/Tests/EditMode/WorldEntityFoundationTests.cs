using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class WorldEntityFoundationTests
    {
        [Test]
        public void Lifecycle_SpawnActivateDespawn_RegistersAndUnregistersEntity()
        {
            GameObject root = new GameObject("World Entity Test");
            WorldEntityRegistry registry = new WorldEntityRegistry();
            try
            {
                WorldEntity entity = root.AddComponent<WorldEntity>();
                entity.Initialize(new EntityId(42), new GlobalPosition(1000d, 12d, -500d),
                    new WorldCoordinateFrame(new GlobalPosition(900d, 0d, -600d)), registry);
                Assert.That(entity.Lifecycle.State, Is.EqualTo(WorldEntityLifecycleState.Created));
                Assert.That(root.transform.position, Is.EqualTo(new Vector3(100f, 12f, 100f)));

                entity.Spawn(); entity.Activate();
                Assert.That(registry.TryGet(new EntityId(42), out WorldEntity registered), Is.True);
                Assert.That(registered, Is.SameAs(entity));
                Assert.That(entity.Lifecycle.State, Is.EqualTo(WorldEntityLifecycleState.Active));

                entity.BeginDespawn(); entity.CompleteDespawn();
                Assert.That(entity.Lifecycle.State, Is.EqualTo(WorldEntityLifecycleState.Destroyed));
                Assert.That(registry.Count, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Presence_RebasePreservesGlobalPositionAndUpdatesUnityLocalPosition()
        {
            GameObject root = new GameObject("World Presence Test");
            try
            {
                WorldPresence presence = new WorldPresence(root.transform, new GlobalPosition(8100d, 25d, -30d),
                    new WorldCoordinateFrame(new GlobalPosition(8000d, 0d, 0d)));
                Assert.That(presence.LocalPosition, Is.EqualTo(new Vector3(100f, 25f, -30f)));

                presence.ApplyCoordinateFrame(new WorldCoordinateFrame(new GlobalPosition(8050d, 0d, -20d)));
                Assert.That(presence.GlobalPosition, Is.EqualTo(new GlobalPosition(8100d, 25d, -30d)));
                Assert.That(presence.LocalPosition, Is.EqualTo(new Vector3(50f, 25f, -10f)));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Registry_DuplicateStableIdentity_IsRejected()
        {
            GameObject firstRoot = new GameObject("First Entity"); GameObject secondRoot = new GameObject("Second Entity");
            WorldEntityRegistry registry = new WorldEntityRegistry();
            try
            {
                WorldEntity first = firstRoot.AddComponent<WorldEntity>(); WorldEntity second = secondRoot.AddComponent<WorldEntity>();
                WorldCoordinateFrame frame = new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d));
                first.Initialize(new EntityId(7), new GlobalPosition(0d, 0d, 0d), frame, registry); first.Spawn();
                second.Initialize(new EntityId(7), new GlobalPosition(1d, 0d, 0d), frame, registry);
                Assert.Throws<System.InvalidOperationException>(() => second.Spawn());
                Assert.That(registry.Count, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(firstRoot); Object.DestroyImmediate(secondRoot); }
        }

        [Test]
        public void Lifecycle_InvalidTransition_IsRejected()
        {
            WorldEntityLifecycle lifecycle = new WorldEntityLifecycle();
            lifecycle.MarkCreated();
            Assert.Throws<System.InvalidOperationException>(() => lifecycle.MarkActive());
        }
    }
}
