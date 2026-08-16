using System;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum EnvironmentalBiomeKind : byte { Grassland = 1, Forest = 2, Desert = 3, Snow = 4 }
    public enum EnvironmentalSurfaceKind : byte { Grass = 1, DrySoil = 2, Rock = 3, Water = 4, Sand = 5, Snow = 6, Mud = 7, Ash = 8, Concrete = 9, Wood = 10 }
    public enum EnvironmentalVfxKind : byte { SandDust = 1, DryLeaves = 2, Pollen = 3, SubtleDebris = 4, LooseSnow = 5, Ash = 6 }
    public enum PhysicalResponseZone : byte { Root = 1, Trunk = 2, LargeBranch = 3, SmallBranch = 4, Leaves = 5, FlexibleSurface = 6 }

    public readonly struct WindSample
    {
        public WindSample(Vector3 direction, float speed, float strength, float turbulence, float gust)
        {
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
            Speed = Mathf.Max(0f, speed); Strength = Mathf.Clamp01(strength);
            Turbulence = Mathf.Clamp01(turbulence); Gust = Mathf.Clamp01(gust);
        }
        public Vector3 Direction { get; }
        public float Speed { get; }
        public float Strength { get; }
        public float Turbulence { get; }
        public float Gust { get; }
        public float EffectiveStrength => Mathf.Clamp01(Strength + Gust);
    }

    [Serializable]
    public sealed class WindProfile
    {
        [SerializeField] private Vector3 _direction = new Vector3(0.82f, 0f, 0.36f);
        [SerializeField, Min(0f)] private float _speed = 4.5f;
        [SerializeField, Range(0f, 1f)] private float _strength = 0.34f;
        [SerializeField, Range(0f, 1f)] private float _turbulence = 0.28f;
        [SerializeField, Range(0f, 1f)] private float _gustStrength = 0.38f;
        [SerializeField, Min(0.01f)] private float _gustFrequency = 0.09f;
        [SerializeField, Min(1f)] private float _spatialScale = 38f;
        [SerializeField, Min(0.001f)] private float _variationSpeed = 0.075f;
        public Vector3 Direction { get => _direction; set => _direction = value; }
        public float Speed { get => _speed; set => _speed = Mathf.Max(0f, value); }
        public float Strength { get => _strength; set => _strength = Mathf.Clamp01(value); }
        public float Turbulence { get => _turbulence; set => _turbulence = Mathf.Clamp01(value); }
        public float GustStrength { get => _gustStrength; set => _gustStrength = Mathf.Clamp01(value); }
        public float GustFrequency { get => _gustFrequency; set => _gustFrequency = Mathf.Max(0.01f, value); }
        public float SpatialScale => _spatialScale;
        public float VariationSpeed => _variationSpeed;
    }

    [Serializable]
    public sealed class PhysicalResponseProfile
    {
        [SerializeField] private float _mass = 1f;
        [SerializeField, Range(0f, 1f)] private float _stiffness = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _flexibility = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _damping = 0.55f;
        [SerializeField, Range(0f, 2f)] private float _windResistance = 0.5f;
        [SerializeField, Min(0f)] private float _impactResistance = 1f;
        [SerializeField, Min(0f)] private float _structuralResistance = 1f;
        [SerializeField, Min(0f)] private float _deformationThreshold = 0.2f;
        [SerializeField, Min(0f)] private float _breakThreshold = 1f;
        [SerializeField, Min(0f)] private float _recoverySpeed = 1f;
        [SerializeField, Min(0f)] private float _surfaceArea = 1f;
        [SerializeField, Min(0f)] private float _dragCoefficient = 0.8f;
        public PhysicalResponseProfile(float mass, float stiffness, float flexibility, float damping, float windResistance,
            float surfaceArea, float dragCoefficient)
        {
            _mass = Mathf.Max(0.01f, mass); _stiffness = Mathf.Clamp01(stiffness); _flexibility = Mathf.Clamp01(flexibility);
            _damping = Mathf.Clamp01(damping); _windResistance = Mathf.Max(0f, windResistance);
            _surfaceArea = Mathf.Max(0f, surfaceArea); _dragCoefficient = Mathf.Max(0f, dragCoefficient);
        }
        public float Mass => _mass; public float Stiffness => _stiffness; public float Flexibility => _flexibility;
        public float Damping => _damping; public float WindResistance => _windResistance;
        public float ImpactResistance => _impactResistance; public float StructuralResistance => _structuralResistance;
        public float DeformationThreshold => _deformationThreshold; public float BreakThreshold => _breakThreshold;
        public float RecoverySpeed => _recoverySpeed; public float SurfaceArea => _surfaceArea; public float DragCoefficient => _dragCoefficient;
        public float ShaderResponse => Mathf.Clamp01(Flexibility * SurfaceArea * DragCoefficient / Mathf.Max(0.2f, Mass) * (1f - Stiffness * 0.5f));
    }

    public readonly struct EnvironmentalVfxRule
    {
        public EnvironmentalVfxRule(EnvironmentalVfxKind kind, float minimumStrength, float density, float speed, float size, float lifetime,
            float rareEventProbability = 0f, float cooldown = 4f)
        { Kind = kind; MinimumStrength = minimumStrength; Density = density; Speed = speed; Size = size; Lifetime = lifetime; RareEventProbability = rareEventProbability; Cooldown = cooldown; }
        public EnvironmentalVfxKind Kind { get; } public float MinimumStrength { get; } public float Density { get; }
        public float Speed { get; } public float Size { get; } public float Lifetime { get; }
        public float RareEventProbability { get; } public float Cooldown { get; }
        public float Evaluate(float strength) { float t = Mathf.InverseLerp(MinimumStrength, 1f, strength); return t * t * (3f - 2f * t); }
    }
}
