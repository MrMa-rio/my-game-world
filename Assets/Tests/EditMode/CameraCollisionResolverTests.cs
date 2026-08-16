using MyGameWorld.Client.PlayerRuntime;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class CameraCollisionResolverTests
    {
        [Test]
        public void Resolve_ObstacleBetweenPivotAndCamera_PullsCameraInFrontOfObstacle()
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CameraCollisionProfile profile = ScriptableObject.CreateInstance<CameraCollisionProfile>();
            try
            {
                obstacle.transform.position = new Vector3(0f, 0f, -2.5f);
                obstacle.transform.localScale = new Vector3(4f, 4f, 0.5f); Physics.SyncTransforms();
                CameraCollisionResolver resolver = new CameraCollisionResolver(profile);
                Vector3 pivot = Vector3.zero; Vector3 desired = new Vector3(0f, 0f, -5f);

                Vector3 resolved = resolver.Resolve(pivot, desired, desired, 0.02f);

                Assert.That(Vector3.Distance(pivot, resolved), Is.LessThan(2.5f));
                Assert.That(Vector3.Distance(pivot, resolved), Is.GreaterThanOrEqualTo(profile.MinimumDistance));
            }
            finally { Object.DestroyImmediate(obstacle); Object.DestroyImmediate(profile); }
        }
    }
}
