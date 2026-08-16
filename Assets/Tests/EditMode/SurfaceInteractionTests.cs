using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class SurfaceInteractionTests
    {
        [Test]
        public void Sample_GroundColliderWithDescriptor_ReturnsSurfaceId()
        {
            GameObject actor = new GameObject("Surface Probe Actor"); GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                actor.transform.position = new Vector3(0f, 1.05f, 0f); CharacterController controller = actor.AddComponent<CharacterController>();
                ground.transform.position = new Vector3(0f, -0.5f, 0f); ground.transform.localScale = new Vector3(10f, 1f, 10f);
                ground.AddComponent<PhysicalSurfaceDescriptor>().Configure(6); Physics.SyncTransforms();
                GroundProbe probe = new GroundProbe(controller, 0.3f, 0.32f, ~0); GroundProbeResult result = probe.Sample();
                Assert.That(result.IsGrounded, Is.True); Assert.That(result.SurfaceId, Is.EqualTo(6));
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(ground); }
        }
    }
}
