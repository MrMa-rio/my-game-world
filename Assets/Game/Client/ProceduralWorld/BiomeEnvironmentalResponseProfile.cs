using System;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    [CreateAssetMenu(fileName = "BiomeEnvironmentalResponseProfile", menuName = "My Game World/Environment/Biome Response Profile")]
    public sealed class BiomeEnvironmentalResponseProfile : ScriptableObject
    {
        [SerializeField] private EnvironmentalBiomeKind _biome = EnvironmentalBiomeKind.Grassland;
        [SerializeField] private SurfaceRule[] _surfaceRules = Array.Empty<SurfaceRule>();
        public EnvironmentalBiomeKind Biome => _biome;

        public bool TryResolve(EnvironmentalSurfaceKind surface, out EnvironmentalVfxRule rule)
        {
            for (int index = 0; index < _surfaceRules.Length; index++)
                if (_surfaceRules[index].Surface == surface) { rule = _surfaceRules[index].ToRule(); return true; }
            rule = default; return false;
        }

        [Serializable]
        private sealed class SurfaceRule
        {
            [SerializeField] private EnvironmentalSurfaceKind _surface = EnvironmentalSurfaceKind.Grass;
            [SerializeField] private EnvironmentalVfxKind _effect = EnvironmentalVfxKind.Pollen;
            [SerializeField, Range(0f, 1f)] private float _minimumWindStrength = 0.1f;
            [SerializeField, Min(0f)] private float _density = 8f;
            [SerializeField, Min(0f)] private float _speedMultiplier = 1f;
            [SerializeField, Min(0.01f)] private float _size = 0.1f;
            [SerializeField, Min(0.1f)] private float _lifetime = 3f;
            [SerializeField, Range(0f, 1f)] private float _eventProbability = 0.05f;
            [SerializeField, Min(0.1f)] private float _eventCooldown = 4f;
            public EnvironmentalSurfaceKind Surface => _surface;
            public EnvironmentalVfxRule ToRule() => new EnvironmentalVfxRule(_effect, _minimumWindStrength, _density,
                _speedMultiplier, _size, _lifetime, _eventProbability, _eventCooldown);
        }
    }
}
