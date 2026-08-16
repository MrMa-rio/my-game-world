using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class PhysicalBodyTests
    {
        [TestCase(PhysicalInteractionLevel.None, false, false)]
        [TestCase(PhysicalInteractionLevel.Soft, true, true)]
        [TestCase(PhysicalInteractionLevel.Solid, true, false)]
        public void Configure_InteractionLevel_ConfiguresCollider(PhysicalInteractionLevel level, bool enabled, bool trigger)
        {
            GameObject root = new GameObject("World Collision Body Test");
            try
            {
                BoxCollider collider = root.AddComponent<BoxCollider>();
                WorldCollisionBody body = root.AddComponent<WorldCollisionBody>();
                body.Configure(level);
                Assert.That(collider.enabled, Is.EqualTo(enabled));
                Assert.That(collider.isTrigger, Is.EqualTo(trigger));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AddCapability_Profile_ConfiguresActorCharacterController()
        {
            GameObject root = new GameObject("Physical Actor Body Test");
            PhysicalBodyProfile profile = ScriptableObject.CreateInstance<PhysicalBodyProfile>();
            try
            {
                Actor actor = CreateActor(root);
                PhysicalBody body = root.AddComponent<PhysicalBody>();
                body.Configure(profile);
                actor.AddCapability<IPhysicalBody>(body);
                CharacterController controller = root.GetComponent<CharacterController>();
                Assert.That(root.layer, Is.EqualTo(WorldPhysicsLayers.Actor));
                Assert.That(controller.height, Is.EqualTo(profile.Height));
                Assert.That(controller.radius, Is.EqualTo(profile.Radius));
                Assert.That(body.Mass, Is.EqualTo(profile.Mass));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1001), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}
