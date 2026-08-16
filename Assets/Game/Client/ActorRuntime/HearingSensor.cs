using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum PerceptionSoundCategory : byte
    {
        General = 0,
        Movement = 1,
        Impact = 2,
        Voice = 3,
        Environment = 4
    }

    public readonly struct PerceptionSoundEvent
    {
        public PerceptionSoundEvent(Vector3 position, float intensity, PerceptionSoundCategory category, GameObject source = null)
        { Position = position; Intensity = Mathf.Max(0f, intensity); Category = category; Source = source; }
        public Vector3 Position { get; }
        public float Intensity { get; }
        public PerceptionSoundCategory Category { get; }
        public GameObject Source { get; }
    }

    public sealed class PerceptionSoundStream
    {
        public event Action<PerceptionSoundEvent> Emitted;
        public void Emit(in PerceptionSoundEvent sound) => Emitted?.Invoke(sound);
    }

    [CreateAssetMenu(menuName = "My Game World/Actor/Hearing Profile")]
    public sealed class HearingProfile : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _baseRange = 30f;
        [SerializeField, Min(0.01f)] private float _sensitivity = 1f;
        public float BaseRange => _baseRange;
        public float Sensitivity => _sensitivity;
    }

    public readonly struct HeardSound
    {
        public HeardSound(PerceptionSoundEvent source, float perceivedIntensity)
        { Source = source; PerceivedIntensity = perceivedIntensity; }
        public PerceptionSoundEvent Source { get; }
        public float PerceivedIntensity { get; }
    }

    public interface IHearingSensor : IActorSensor
    {
        event Action<HeardSound> SoundHeard;
    }

    [DisallowMultipleComponent]
    public sealed class HearingSensor : ActorSensor, IHearingSensor
    {
        [SerializeField] private HearingProfile _profile;
        private PerceptionSoundStream _stream;
        public event Action<HeardSound> SoundHeard;
        public HearingProfile Profile => _profile;

        public void Configure(HearingProfile profile, PerceptionSoundStream stream)
        {
            if (IsInitialized) throw new InvalidOperationException("Hearing configuration cannot change after initialization.");
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.EventDriven) throw new InvalidOperationException("HearingSensor must be event-driven.");
            if (_profile == null || _stream == null) throw new InvalidOperationException("HearingSensor requires profile and sound stream.");
            _stream.Emitted += OnSoundEmitted;
        }

        protected override void OnReleasing()
        {
            if (_stream != null) _stream.Emitted -= OnSoundEmitted;
        }

        protected override void Sample() { }

        private void OnSoundEmitted(PerceptionSoundEvent sound)
        {
            if (!IsEnabled || !Context.Actor.State.CanAct || sound.Intensity <= 0f) return;
            float range = _profile.BaseRange * _profile.Sensitivity * sound.Intensity;
            float distance = Vector3.Distance(Context.Transform.position, sound.Position);
            if (distance > range) return;
            float perceived = range > 0f ? Mathf.Clamp01(1f - distance / range) * sound.Intensity : 0f;
            SoundHeard?.Invoke(new HeardSound(sound, perceived));
        }
    }
}
