using UnityEngine;
using MyGameWorld.Client.EntityRuntime;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct GroundProbeResult
    {
        public GroundProbeResult(bool isGrounded, Vector3 normal, float distance, Collider collider, int surfaceId = 0)
        { IsGrounded = isGrounded; Normal = normal; Distance = distance; Collider = collider; SurfaceId = surfaceId; }
        public bool IsGrounded { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public Collider Collider { get; }
        public int SurfaceId { get; }
        public float Angle => IsGrounded ? Vector3.Angle(Vector3.up, Normal) : 90f;
        public static GroundProbeResult Airborne => new GroundProbeResult(false, Vector3.up, float.PositiveInfinity, null);
    }

    public sealed class GroundProbe
    {
        private readonly CharacterController _controller;
        private readonly float _distance;
        private readonly float _radius;
        private readonly int _layerMask;

        public GroundProbe(CharacterController controller, float distance, float radius, int layerMask)
        { _controller = controller; _distance = distance; _radius = Mathf.Min(radius, controller.radius * 0.95f); _layerMask = layerMask; }

        public GroundProbeResult Sample()
        {
            Vector3 center = _controller.transform.TransformPoint(_controller.center);
            float bottomOffset = Mathf.Max(0f, _controller.height * 0.5f - _radius);
            Vector3 origin = center + Vector3.down * bottomOffset + Vector3.up * 0.05f;
            RaycastHit hit;
            if (!Physics.SphereCast(origin, _radius, Vector3.down, out hit, _distance + 0.05f, _layerMask, QueryTriggerInteraction.Ignore))
                return GroundProbeResult.Airborne;
            IPhysicalSurfaceProvider surface = hit.collider.GetComponentInParent<IPhysicalSurfaceProvider>();
            return new GroundProbeResult(true, hit.normal, Mathf.Max(0f, hit.distance - 0.05f), hit.collider,
                surface != null ? surface.SurfaceId : 0);
        }
    }
}
