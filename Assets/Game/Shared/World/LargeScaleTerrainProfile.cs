using System;

namespace MyGameWorld.Shared.World
{
    public sealed class LargeScaleTerrainProfile
    {
        public LargeScaleTerrainProfile(ushort version, float mountainScale, float mountainAmplitude,
            float ridgeSharpness, float valleyScale, float valleyAmplitude)
        {
            if (version == 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (mountainScale <= 0f || valleyScale <= 0f || mountainAmplitude < 0f || valleyAmplitude < 0f)
                throw new ArgumentOutOfRangeException();
            Version = version; MountainScale = mountainScale; MountainAmplitude = mountainAmplitude;
            RidgeSharpness = Math.Max(1f, ridgeSharpness); ValleyScale = valleyScale; ValleyAmplitude = valleyAmplitude;
        }
        public ushort Version { get; }
        public float MountainScale { get; }
        public float MountainAmplitude { get; }
        public float RidgeSharpness { get; }
        public float ValleyScale { get; }
        public float ValleyAmplitude { get; }

        public static LargeScaleTerrainProfile CreateScalableHighlands() => new LargeScaleTerrainProfile(
            version: 1, mountainScale: 7200f, mountainAmplitude: 310f, ridgeSharpness: 2.6f,
            valleyScale: 14500f, valleyAmplitude: 70f);

        public static LargeScaleTerrainProfile CreateGeologicalHighlands() => new LargeScaleTerrainProfile(
            version: 2, mountainScale: 7200f, mountainAmplitude: 310f, ridgeSharpness: 2.6f,
            valleyScale: 14500f, valleyAmplitude: 70f);
    }
}
