using System;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum DayPhase : byte { DeepNight = 1, Dawn = 2, Day = 3, Dusk = 4, Night = 5 }

    [Serializable]
    public sealed class WorldTimeProfile
    {
        [SerializeField, Min(10f)] private float _realSecondsPerDay = 300f;
        [SerializeField, Range(0f, 24f)] private float _startHour = 5.5f;
        [SerializeField, Range(0f, 20f)] private float _timeScale = 1f;
        [SerializeField] private bool _paused;
        public float RealSecondsPerDay => _realSecondsPerDay;
        public float StartHour => _startHour;
        public float TimeScale { get => _timeScale; set => _timeScale = Mathf.Clamp(value, 0f, 20f); }
        public bool Paused { get => _paused; set => _paused = value; }
    }

    public readonly struct WorldTimeSnapshot
    {
        public WorldTimeSnapshot(double totalHours)
        {
            TotalHours = totalHours; Hour = (float)(totalHours % 24d); if (Hour < 0f) Hour += 24f;
            NormalizedDay = Hour / 24f; DayIndex = (long)Math.Floor(totalHours / 24d);
            Phase = ResolvePhase(Hour);
            Daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.25f, 7.25f, Hour)) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(17.25f, 19.25f, Hour)));
            Night = 1f - Daylight;
            Dawn = Mathf.Clamp01(1f - Mathf.Abs(Hour - 6.25f) / 1.65f);
            Dusk = Mathf.Clamp01(1f - Mathf.Abs(Hour - 18.25f) / 1.65f);
        }
        public double TotalHours { get; } public float Hour { get; } public float NormalizedDay { get; }
        public long DayIndex { get; } public DayPhase Phase { get; } public float Daylight { get; }
        public float Night { get; } public float Dawn { get; } public float Dusk { get; }
        private static DayPhase ResolvePhase(float hour)
        {
            if (hour < 4.75f) return DayPhase.DeepNight; if (hour < 7.25f) return DayPhase.Dawn;
            if (hour < 17.25f) return DayPhase.Day; if (hour < 19.5f) return DayPhase.Dusk; return DayPhase.Night;
        }
    }

    public sealed class WorldTimeSystem
    {
        private readonly WorldTimeProfile _profile; private double _totalHours;
        public WorldTimeSystem(WorldTimeProfile profile) { _profile = profile ?? new WorldTimeProfile(); _totalHours = _profile.StartHour; Snapshot = new WorldTimeSnapshot(_totalHours); }
        public WorldTimeProfile Profile => _profile; public WorldTimeSnapshot Snapshot { get; private set; }
        public void Tick(float unscaledDeltaTime)
        {
            if (!_profile.Paused) _totalHours += Math.Max(0f, unscaledDeltaTime) * _profile.TimeScale * 24d / _profile.RealSecondsPerDay;
            Snapshot = new WorldTimeSnapshot(_totalHours);
        }
        public void SetHour(float hour) { _totalHours = Math.Floor(_totalHours / 24d) * 24d + Mathf.Repeat(hour, 24f); Snapshot = new WorldTimeSnapshot(_totalHours); }
        public void AdvanceHours(float hours) { _totalHours += hours; Snapshot = new WorldTimeSnapshot(_totalHours); }
    }
}
