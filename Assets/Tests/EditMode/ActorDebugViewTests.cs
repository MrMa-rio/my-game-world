using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorDebugViewTests
    {
        [Test]
        public void RefreshSnapshot_ActorComposition_ExposesStableDebugData()
        {
            GameObject root = new GameObject("Actor Debug Test");
            try
            {
                Actor actor = CreateActor(root); ActorDebugView debug = root.AddComponent<ActorDebugView>(); debug.Initialize(actor); debug.RefreshSnapshot();
                Assert.That(debug.Snapshot.EntityId, Is.EqualTo(3101)); Assert.That(debug.Snapshot.WorldPosition.X, Is.EqualTo(4d));
                Assert.That(debug.Snapshot.Controller, Is.EqualTo("None")); Assert.That(debug.Snapshot.CapabilityCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>(); entity.Initialize(new EntityId(3101), new GlobalPosition(4d, 0d, 8d),
                new WorldCoordinateFrame(new GlobalPosition()), new WorldEntityRegistry()); entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
