using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [DisallowMultipleComponent]
    public sealed class ActorSensorScheduler : MonoBehaviour
    {
        private readonly List<ActorSensor> _sensors = new List<ActorSensor>();
        public int RegisteredCount => _sensors.Count;

        public void Register(ActorSensor sensor)
        {
            if (sensor == null) throw new ArgumentNullException(nameof(sensor));
            if (_sensors.Contains(sensor)) throw new InvalidOperationException("Sensor is already scheduled.");
            _sensors.Add(sensor);
        }

        public void Unregister(ActorSensor sensor) => _sensors.Remove(sensor);
        public void TickIntervals(float deltaTime) => Tick(deltaTime, SensorTickMode.Interval);
        public void TickPhysics(float deltaTime) => Tick(deltaTime, SensorTickMode.Physics);

        private void Update() => TickIntervals(Time.deltaTime);
        private void FixedUpdate() => TickPhysics(Time.fixedDeltaTime);

        private void Tick(float deltaTime, SensorTickMode mode)
        {
            if (deltaTime <= 0f) return;
            for (int index = _sensors.Count - 1; index >= 0; index--)
            {
                ActorSensor sensor = _sensors[index];
                if (sensor == null) { _sensors.RemoveAt(index); continue; }
                sensor.Advance(deltaTime, mode);
            }
        }
    }
}
