using System;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Player/Camera Collision Profile")]
    public sealed class CameraCollisionProfile : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _radius = 0.25f;
        [SerializeField, Min(0f)] private float _padding = 0.12f;
        [SerializeField, Min(0.05f)] private float _minimumDistance = 0.35f;
        [SerializeField, Min(0f)] private float _returnSpeed = 12f;
        [SerializeField] private LayerMask _collisionLayers = ~0;
        public float Radius => _radius;
        public float Padding => _padding;
        public float MinimumDistance => _minimumDistance;
        public float ReturnSpeed => _returnSpeed;
        public int CollisionLayers => _collisionLayers;
    }

    public sealed class CameraCollisionResolver
    {
        private readonly CameraCollisionProfile _profile;
        public CameraCollisionResolver(CameraCollisionProfile profile)
            => _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));

        public Vector3 Resolve(Vector3 pivot, Vector3 desiredPosition, Vector3 currentPosition, float deltaTime)
        {
            Vector3 offset = desiredPosition - pivot; float desiredDistance = offset.magnitude;
            if (desiredDistance <= 0.0001f) return pivot;
            Vector3 direction = offset / desiredDistance;
            if (Physics.SphereCast(pivot, _profile.Radius, direction, out RaycastHit hit, desiredDistance,
                _profile.CollisionLayers, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Clamp(hit.distance - _profile.Padding, _profile.MinimumDistance, desiredDistance);
                return pivot + direction * safeDistance;
            }
            if (_profile.ReturnSpeed <= 0f || deltaTime <= 0f) return desiredPosition;
            return Vector3.MoveTowards(currentPosition, desiredPosition, _profile.ReturnSpeed * deltaTime);
        }
    }
}
