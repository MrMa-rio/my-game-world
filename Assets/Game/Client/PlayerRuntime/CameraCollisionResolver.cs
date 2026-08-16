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
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private float _currentDistance = -1f;
        public CameraCollisionResolver(CameraCollisionProfile profile)
            => _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));

        public void Reset(float distance) => _currentDistance = Mathf.Max(0f, distance);

        public Vector3 Resolve(Vector3 pivot, Vector3 desiredPosition, float deltaTime, Transform ignoredRoot = null)
        {
            Vector3 offset = desiredPosition - pivot; float desiredDistance = offset.magnitude;
            if (desiredDistance <= 0.0001f) return pivot;
            Vector3 direction = offset / desiredDistance;
            float obstacleDistance = FindNearestObstacle(pivot, direction, desiredDistance, ignoredRoot);
            if (obstacleDistance >= 0f)
            {
                _currentDistance = Mathf.Clamp(obstacleDistance - _profile.Padding, _profile.MinimumDistance, desiredDistance);
            }
            else if (_currentDistance < 0f || _profile.ReturnSpeed <= 0f || deltaTime <= 0f)
            {
                _currentDistance = desiredDistance;
            }
            else
            {
                _currentDistance = Mathf.MoveTowards(_currentDistance, desiredDistance, _profile.ReturnSpeed * deltaTime);
            }
            return pivot + direction * Mathf.Min(_currentDistance, desiredDistance);
        }

        private float FindNearestObstacle(Vector3 pivot, Vector3 direction, float distance, Transform ignoredRoot)
        {
            int count = Physics.SphereCastNonAlloc(pivot, _profile.Radius, direction, _hits, distance,
                _profile.CollisionLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int index = 0; index < count; index++)
            {
                Collider collider = _hits[index].collider;
                if (collider == null || IsPartOf(collider.transform, ignoredRoot)) continue;
                nearest = Mathf.Min(nearest, _hits[index].distance);
            }
            return float.IsPositiveInfinity(nearest) ? -1f : nearest;
        }

        private static bool IsPartOf(Transform candidate, Transform root)
            => root != null && (candidate == root || candidate.IsChildOf(root));
    }
}
