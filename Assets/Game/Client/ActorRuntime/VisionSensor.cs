using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IVisionTarget
    {
        Transform VisionTransform { get; }
        bool IsVisible { get; }
    }

    [DisallowMultipleComponent]
    public sealed class VisionTarget : MonoBehaviour, IVisionTarget
    {
        [SerializeField] private bool _isVisible = true;
        public Transform VisionTransform => transform;
        public bool IsVisible => _isVisible && isActiveAndEnabled;
        public void SetVisible(bool visible) => _isVisible = visible;
    }

    public interface IVisionSensor : IActorSensor
    {
        IReadOnlyList<IVisionTarget> Detected { get; }
        event Action<IReadOnlyList<IVisionTarget>> PerceptionUpdated;
    }

    [DisallowMultipleComponent]
    public sealed class VisionSensor : ActorSensor, IVisionSensor
    {
        [SerializeField] private VisionProfile _profile;
        private Collider[] _candidates;
        private readonly List<IVisionTarget> _detected = new List<IVisionTarget>();
        private readonly HashSet<IVisionTarget> _unique = new HashSet<IVisionTarget>();

        public IReadOnlyList<IVisionTarget> Detected => _detected;
        public VisionProfile Profile => _profile;
        public event Action<IReadOnlyList<IVisionTarget>> PerceptionUpdated;

        public void Configure(VisionProfile profile)
        {
            if (IsInitialized) throw new InvalidOperationException("Vision configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        }

        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.Interval) throw new InvalidOperationException("VisionSensor must use interval scheduling.");
            if (_profile == null) throw new InvalidOperationException("VisionSensor requires a VisionProfile.");
            _candidates = new Collider[_profile.CandidateCapacity];
        }

        protected override void Sample()
        {
            _detected.Clear(); _unique.Clear();
            Vector3 eye = Context.Transform.position + Vector3.up * _profile.EyeHeight;
            int count = Physics.OverlapSphereNonAlloc(eye, _profile.Range, _candidates,
                _profile.VisibleLayers, QueryTriggerInteraction.Collide);
            float halfFov = _profile.FieldOfView * 0.5f;
            for (int index = 0; index < count; index++)
            {
                Collider candidate = _candidates[index];
                if (candidate == null || candidate.transform.IsChildOf(Context.Transform)) continue;
                IVisionTarget target = candidate.GetComponentInParent<IVisionTarget>();
                if (target == null || !target.IsVisible || !_unique.Add(target)) continue;
                Vector3 targetPosition = target.VisionTransform.position;
                Vector3 delta = targetPosition - eye;
                if (delta.sqrMagnitude < 0.0001f || Vector3.Angle(Context.Transform.forward, delta) > halfFov) continue;
                if (Physics.Raycast(eye, delta.normalized, out RaycastHit hit, delta.magnitude,
                    _profile.OcclusionLayers, QueryTriggerInteraction.Ignore))
                {
                    IVisionTarget hitTarget = hit.collider.GetComponentInParent<IVisionTarget>();
                    if (!ReferenceEquals(hitTarget, target)) continue;
                }
                _detected.Add(target);
            }
            PerceptionUpdated?.Invoke(_detected);
        }
    }
}
