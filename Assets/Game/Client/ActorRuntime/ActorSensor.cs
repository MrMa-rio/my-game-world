using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum SensorTickMode : byte
    {
        EventDriven = 0,
        Interval = 1,
        Physics = 2
    }

    public interface IActorSensor
    {
        ActorContext Context { get; }
        bool IsInitialized { get; }
        bool IsEnabled { get; }
        SensorTickMode TickMode { get; }
        float Interval { get; }
        void Initialize(ActorContext context);
        void SetEnabled(bool enabled);
        void Release();
    }

    public abstract class ActorSensor : MonoBehaviour, IActorSensor
    {
        [SerializeField] private bool _initiallyEnabled = true;
        [SerializeField] private SensorTickMode _tickMode = SensorTickMode.Interval;
        [SerializeField, Min(0.01f)] private float _interval = 0.2f;
        private ActorSensorScheduler _scheduler;
        private float _elapsed;
        private float _sampleDeltaTime;

        public ActorContext Context { get; private set; }
        public bool IsInitialized => Context != null;
        public bool IsEnabled { get; private set; }
        public SensorTickMode TickMode => _tickMode;
        public float Interval => Mathf.Max(0.01f, _interval);
        protected float SampleDeltaTime => _sampleDeltaTime;

        public void ConfigureScheduling(SensorTickMode tickMode, float interval = 0.2f,
            ActorSensorScheduler scheduler = null)
        {
            if (IsInitialized) throw new InvalidOperationException("Sensor scheduling cannot change after initialization.");
            _tickMode = tickMode;
            _interval = Mathf.Max(0.01f, interval);
            _scheduler = scheduler;
        }

        public void Initialize(ActorContext context)
        {
            if (IsInitialized) throw new InvalidOperationException($"Sensor {GetType().Name} is already initialized.");
            Context = context ?? throw new ArgumentNullException(nameof(context));
            IsEnabled = _initiallyEnabled;
            try
            {
                OnInitialized();
                if (_tickMode != SensorTickMode.EventDriven) _scheduler?.Register(this);
            }
            catch
            {
                _scheduler?.Unregister(this);
                OnReleasing();
                Context = null;
                IsEnabled = false;
                throw;
            }
        }

        public void SetEnabled(bool enabled)
        {
            if (!IsInitialized) throw new InvalidOperationException($"Sensor {GetType().Name} is not initialized.");
            IsEnabled = enabled;
        }

        public void Release()
        {
            if (!IsInitialized) return;
            _scheduler?.Unregister(this);
            OnReleasing();
            Context = null;
            IsEnabled = false;
            _elapsed = 0f;
        }

        internal void Advance(float deltaTime, SensorTickMode tickMode)
        {
            if (!IsInitialized || !IsEnabled || !Context.Actor.State.CanAct || _tickMode != tickMode) return;
            if (tickMode == SensorTickMode.Physics)
            {
                _sampleDeltaTime = deltaTime;
                Sample();
                return;
            }
            _elapsed += deltaTime;
            if (_elapsed < Interval) return;
            _elapsed %= Interval;
            _sampleDeltaTime = deltaTime;
            Sample();
        }

        protected abstract void Sample();
        protected virtual void OnInitialized() { }
        protected virtual void OnReleasing() { }
        protected virtual void OnDestroy() => Release();
    }
}
