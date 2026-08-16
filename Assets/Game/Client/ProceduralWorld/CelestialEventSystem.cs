using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum CelestialEventKind : byte { ShootingStar = 1, Meteor = 2 }

    public sealed class CelestialEventSystem : IDisposable
    {
        private readonly List<CelestialEventInstance> _instances = new List<CelestialEventInstance>();
        private readonly DeterministicRandom _random; private readonly Material _shootingStar; private readonly Material _meteor;
        private float _elapsed; private float _nextEvent;
        public CelestialEventSystem(Transform parent, long seed, int capacity = 4)
        {
            _random = new DeterministicRandom(SeedDerivation.Derive(seed, 0x534B5945, 1));
            Shader shader = Shader.Find("MyGameWorld/Procedural World/Celestial Event");
            if (shader == null) throw new InvalidOperationException("Celestial event shader was not found.");
            _shootingStar = CreateMaterial(shader, "Shooting Star", new Color(0.68f, 0.82f, 1f, 0.92f));
            _meteor = CreateMaterial(shader, "Meteor", new Color(1f, 0.32f, 0.06f, 0.95f));
            for (int index = 0; index < Mathf.Max(1, capacity); index++) _instances.Add(new CelestialEventInstance(parent, index));
            ScheduleNext();
        }
        public int ActiveCount { get { int count = 0; for (int i = 0; i < _instances.Count; i++) if (_instances[i].Active) count++; return count; } }

        public void Tick(float deltaTime, WorldTimeSnapshot time, Camera camera)
        {
            _elapsed += Mathf.Max(0f, deltaTime);
            for (int index = 0; index < _instances.Count; index++) _instances[index].Tick(deltaTime);
            if (camera == null || time.StarVisibility < 0.72f || _elapsed < _nextEvent) return;
            CelestialEventKind kind = _random.NextUnitDouble() < 0.1d ? CelestialEventKind.Meteor : CelestialEventKind.ShootingStar;
            Spawn(kind, camera); ScheduleNext();
        }

        public bool Spawn(CelestialEventKind kind, Camera camera)
        {
            if (camera == null) return false;
            for (int index = 0; index < _instances.Count; index++) if (!_instances[index].Active)
            {
                float side = (float)_random.NextUnitDouble() * 2f - 1f; float height = 90f + (float)_random.NextUnitDouble() * 50f;
                Vector3 start = camera.transform.position + camera.transform.forward * 190f + camera.transform.right * side * 130f + Vector3.up * height;
                Vector3 velocity = (camera.transform.right * (side > 0f ? -1f : 1f) + Vector3.down * 0.28f).normalized * (kind == CelestialEventKind.Meteor ? 95f : 145f);
                _instances[index].Play(kind, start, velocity, kind == CelestialEventKind.Meteor ? _meteor : _shootingStar); return true;
            }
            return false;
        }

        private void ScheduleNext() { _nextEvent = _elapsed + Mathf.Lerp(7f, 18f, (float)_random.NextUnitDouble()); }
        public void Dispose()
        {
            for (int i = 0; i < _instances.Count; i++) _instances[i].Dispose(); _instances.Clear();
            if (_shootingStar != null) UnityEngine.Object.Destroy(_shootingStar); if (_meteor != null) UnityEngine.Object.Destroy(_meteor);
        }
        private static Material CreateMaterial(Shader shader, string name, Color color) { Material material = new Material(shader) { name = name }; material.SetColor("_BaseColor", color); return material; }
    }

    internal sealed class CelestialEventInstance : IDisposable
    {
        private readonly GameObject _root; private readonly TrailRenderer _trail; private Vector3 _velocity; private float _remaining;
        public CelestialEventInstance(Transform parent, int index)
        {
            _root = new GameObject($"Celestial Event {index}"); _root.transform.SetParent(parent, false);
            _trail = _root.AddComponent<TrailRenderer>(); _trail.time = 0.72f; _trail.minVertexDistance = 1.2f;
            _trail.widthCurve = AnimationCurve.Linear(0f, 0.04f, 1f, 0.75f); _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.receiveShadows = false; _root.SetActive(false);
        }
        public bool Active => _root.activeSelf;
        public void Play(CelestialEventKind kind, Vector3 position, Vector3 velocity, Material material)
        {
            _root.SetActive(true); _root.transform.position = position; _velocity = velocity;
            _remaining = kind == CelestialEventKind.Meteor ? 2.2f : 1.35f; _trail.time = kind == CelestialEventKind.Meteor ? 1.1f : 0.62f;
            _trail.widthMultiplier = kind == CelestialEventKind.Meteor ? 2.4f : 1f; _trail.sharedMaterial = material; _trail.Clear();
        }
        public void Tick(float deltaTime)
        {
            if (!Active) return; _root.transform.position += _velocity * deltaTime; _remaining -= deltaTime;
            if (_remaining <= 0f) _root.SetActive(false);
        }
        public void Dispose() { if (_root != null) UnityEngine.Object.Destroy(_root); }
    }
}
