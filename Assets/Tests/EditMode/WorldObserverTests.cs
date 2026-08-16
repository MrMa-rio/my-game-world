using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class WorldObserverTests
    {
        [Test]
        public void Initialize_PlayerObserver_RegistersRelevanceWithoutLoadingWorld()
        {
            GameObject root = new GameObject("World Observer Player Test"); WorldObserverRegistry registry = new WorldObserverRegistry();
            try
            {
                Actor actor = CreateActor(root); PlayerWorldObserverSystem system = root.AddComponent<PlayerWorldObserverSystem>();
                system.Initialize(actor, registry);
                Assert.That(registry.Count, Is.EqualTo(1)); Assert.That(system.Observer.GlobalPosition.X, Is.EqualTo(10d));
                Assert.That((system.Observer.Purpose & WorldObserverPurpose.Streaming) != 0, Is.True);
                system.Release(); Assert.That(registry.Count, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>(); entity.Initialize(new EntityId(2601), new GlobalPosition(10d, 0d, 20d),
                new WorldCoordinateFrame(new GlobalPosition()), new WorldEntityRegistry()); entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
