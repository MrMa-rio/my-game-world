using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class WindSystem
    {
        private static readonly int WindDirectionStrength = Shader.PropertyToID("_WorldWindDirectionStrength");
        private static readonly int WindParameters = Shader.PropertyToID("_WorldWindParameters");
        private readonly WindProfile _profile;
        private readonly float _seedOffset;
        private float _time;

        public WindSystem(WindProfile profile, long seed)
        {
            _profile = profile ?? new WindProfile();
            uint bits = unchecked((uint)(seed ^ (seed >> 32)));
            _seedOffset = (bits & 65535u) * 0.0137f;
        }
        public WindProfile Profile => _profile;
        public WindSample GlobalSample { get; private set; }

        public void Tick(float deltaTime)
        {
            _time += Mathf.Max(0f, deltaTime);
            GlobalSample = SampleWind(Vector3.zero);
            Vector3 direction = GlobalSample.Direction;
            Shader.SetGlobalVector(WindDirectionStrength, new Vector4(direction.x, direction.y, direction.z, GlobalSample.EffectiveStrength));
            Shader.SetGlobalVector(WindParameters, new Vector4(GlobalSample.Speed, GlobalSample.Turbulence, GlobalSample.Gust, _time));
        }

        public WindSample SampleWind(Vector3 worldPosition)
        {
            float spatial = 1f / Mathf.Max(1f, _profile.SpatialScale);
            float temporal = _time * _profile.VariationSpeed;
            float fieldA = Mathf.PerlinNoise(_seedOffset + worldPosition.x * spatial + temporal, 17.3f + worldPosition.z * spatial);
            float fieldB = Mathf.PerlinNoise(43.1f + worldPosition.z * spatial - temporal * 0.73f, _seedOffset + worldPosition.x * spatial);
            float gustNoise = Mathf.PerlinNoise(_seedOffset * 0.31f + _time * _profile.GustFrequency, 91.7f);
            float gust = Mathf.SmoothStep(0.68f, 0.94f, gustNoise) * _profile.GustStrength;
            float angle = (fieldA - 0.5f) * _profile.Turbulence * 0.9f;
            Vector3 baseDirection = _profile.Direction.sqrMagnitude > 0.001f ? _profile.Direction.normalized : Vector3.right;
            Vector3 direction = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f) * baseDirection;
            float localStrength = Mathf.Clamp01(_profile.Strength * Mathf.Lerp(0.78f, 1.22f, fieldB));
            return new WindSample(direction, _profile.Speed * Mathf.Lerp(0.85f, 1.15f, fieldA), localStrength,
                _profile.Turbulence * Mathf.Lerp(0.65f, 1f, fieldB), gust);
        }
    }
}
