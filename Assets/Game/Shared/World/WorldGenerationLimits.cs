using System;

namespace MyGameWorld.Shared.World
{
    public sealed class WorldGenerationLimits
    {
        public WorldGenerationLimits(int maxLandforms = 8, int maxPaths = 4, int maxDecorations = 96,
            float maxLandformRadius = 30f, float maxLandformAmplitude = 8f, float boundaryPadding = 5f)
        {
            if (maxLandforms < 1 || maxPaths < 0 || maxDecorations < 1) throw new ArgumentOutOfRangeException();
            if (maxLandformRadius <= 0f || maxLandformAmplitude <= 0f || boundaryPadding < 0f) throw new ArgumentOutOfRangeException();
            MaxLandforms = maxLandforms; MaxPaths = maxPaths; MaxDecorations = maxDecorations;
            MaxLandformRadius = maxLandformRadius; MaxLandformAmplitude = maxLandformAmplitude; BoundaryPadding = boundaryPadding;
        }

        public int MaxLandforms { get; }
        public int MaxPaths { get; }
        public int MaxDecorations { get; }
        public float MaxLandformRadius { get; }
        public float MaxLandformAmplitude { get; }
        public float BoundaryPadding { get; }
        public static WorldGenerationLimits Sandbox => new WorldGenerationLimits(
            maxLandforms: 32,
            maxPaths: 10,
            maxDecorations: 1200,
            maxLandformRadius: 90f,
            maxLandformAmplitude: 18f,
            boundaryPadding: 15f);

        public static WorldGenerationLimits LargeSandbox => new WorldGenerationLimits(
            maxLandforms: 96,
            maxPaths: 24,
            maxDecorations: 2400,
            maxLandformRadius: 360f,
            maxLandformAmplitude: 75f,
            boundaryPadding: 60f);

        public static WorldGenerationLimits LegacySandbox => new WorldGenerationLimits();
    }
}
