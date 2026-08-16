using System;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class TouchSensorTests
    {
        [Test]
        public void PhysicalContact_Event_ProducesTouchEventWithoutPolling()
        {
            GameObject actorRoot = new GameObject("Touch Actor Test");
            GameObject surfaceRoot = new GameObject("Touch Surface Test");
            try
            {
                Actor actor = CreateActor(actorRoot);
                StubPhysicalBody body = actorRoot.AddComponent<StubPhysicalBody>();
                actor.AddCapability<IPhysicalBody>(body);
                TouchSensor touch = actorRoot.AddComponent<TouchSensor>();
                touch.ConfigureScheduling(SensorTickMode.EventDriven);
                actor.AddSensor<ITouchSensor>(touch);
                BoxCollider collider = surfaceRoot.AddComponent<BoxCollider>();
                TestSurface surface = surfaceRoot.AddComponent<TestSurface>(); surface.SurfaceIdValue = 7;
                int eventCount = 0; touch.ContactSensed += _ => eventCount++;

                body.Raise(new PhysicalContact(collider, PhysicalInteractionLevel.Soft, Vector3.one,
                    Vector3.up, new Vector3(2f, 0f, 0f)));

                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(touch.LastContact.Source, Is.SameAs(surfaceRoot));
                Assert.That(touch.LastContact.SurfaceId, Is.EqualTo(7));
                Assert.That(touch.LastContact.Force, Is.EqualTo(20f));
                Assert.That(touch.LastContact.Interaction, Is.EqualTo(PhysicalInteractionLevel.Soft));
            }
            finally { UnityEngine.Object.DestroyImmediate(actorRoot); UnityEngine.Object.DestroyImmediate(surfaceRoot); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1301), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private sealed class StubPhysicalBody : ActorCapability, IPhysicalBody
        {
            public float Mass => 10f;
            public event Action<PhysicalContact> Contacted;
            public void Raise(PhysicalContact contact) => Contacted?.Invoke(contact);
        }

        private sealed class TestSurface : MonoBehaviour, IPhysicalSurfaceProvider
        {
            public int SurfaceIdValue { get; set; }
            public int SurfaceId => SurfaceIdValue;
        }
    }
}
