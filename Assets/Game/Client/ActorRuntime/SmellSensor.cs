using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct ScentSource
    {
        public ScentSource(int scentId, Vector3 position, float intensity, GameObject source = null)
        { ScentId = scentId; Position = position; Intensity = Mathf.Max(0f, intensity); Source = source; }
        public int ScentId { get; }
        public Vector3 Position { get; }
        public float Intensity { get; }
        public GameObject Source { get; }
    }

    public sealed class ScentField
    {
        private readonly Dictionary<object, ScentSource> _sources = new Dictionary<object, ScentSource>();
        public IEnumerable<ScentSource> Sources => _sources.Values;
        public void Set(object key, in ScentSource source)
        { if (key == null) throw new ArgumentNullException(nameof(key)); _sources[key] = source; }
        public bool Remove(object key) => key != null && _sources.Remove(key);
    }

    public interface IScentTransport
    {
        float ResolveIntensity(in ScentSource source, Vector3 observerPosition, float baseIntensity);
    }

    [CreateAssetMenu(menuName = "My Game World/Actor/Smell Profile")]
    public sealed class SmellProfile : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _range = 12f;
        [SerializeField, Min(0.01f)] private float _sensitivity = 1f;
        public float Range => _range;
        public float Sensitivity => _sensitivity;
    }

    public readonly struct DetectedScent
    {
        public DetectedScent(ScentSource source, float intensity) { Source = source; Intensity = intensity; }
        public ScentSource Source { get; }
        public float Intensity { get; }
    }

    public interface ISmellSensor : IActorSensor
    {
        IReadOnlyList<DetectedScent> Detected { get; }
        event Action<IReadOnlyList<DetectedScent>> PerceptionUpdated;
    }

    [DisallowMultipleComponent]
    public sealed class SmellSensor : ActorSensor, ISmellSensor
    {
        [SerializeField] private SmellProfile _profile;
        private ScentField _field;
        private IScentTransport _transport;
        private readonly List<DetectedScent> _detected = new List<DetectedScent>();
        public IReadOnlyList<DetectedScent> Detected => _detected;
        public SmellProfile Profile => _profile;
        public event Action<IReadOnlyList<DetectedScent>> PerceptionUpdated;

        public void Configure(SmellProfile profile, ScentField field, IScentTransport transport = null)
        {
            if (IsInitialized) throw new InvalidOperationException("Smell configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            _field = field ?? throw new ArgumentNullException(nameof(field)); _transport = transport;
        }

        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.Interval) throw new InvalidOperationException("SmellSensor must use interval scheduling.");
            if (_profile == null || _field == null) throw new InvalidOperationException("SmellSensor requires profile and scent field.");
        }

        protected override void Sample()
        {
            _detected.Clear(); Vector3 observer = Context.Transform.position;
            foreach (ScentSource source in _field.Sources)
            {
                float distance = Vector3.Distance(observer, source.Position);
                if (distance > _profile.Range || source.Intensity <= 0f) continue;
                float intensity = source.Intensity * _profile.Sensitivity * (1f - distance / _profile.Range);
                if (_transport != null) intensity = _transport.ResolveIntensity(in source, observer, intensity);
                if (intensity > 0f) _detected.Add(new DetectedScent(source, intensity));
            }
            PerceptionUpdated?.Invoke(_detected);
        }
    }
}
