using System;
using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum ProceduralVisualLod : byte
    {
        High = 0,
        Medium = 1,
        Low = 2
    }

    public enum GenerationPriority : byte
    {
        High = 0,
        Normal = 1,
        Low = 2
    }

    public readonly struct ProceduralEnvironmentContext
    {
        public ProceduralEnvironmentContext(BiomeId biome, Vector3 surfaceNormal, float slope, float altitude)
        {
            Biome = biome;
            SurfaceNormal = surfaceNormal;
            Slope = Mathf.Clamp01(slope);
            Altitude = altitude;
        }

        public BiomeId Biome { get; }
        public Vector3 SurfaceNormal { get; }
        public float Slope { get; }
        public float Altitude { get; }
    }

    public readonly struct ProceduralGenerationRequest
    {
        public ProceduralGenerationRequest(
            DecorationPlacement definition,
            ProceduralEnvironmentContext environment,
            ProceduralVisualLod desiredLod,
            GenerationPriority priority)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Environment = environment;
            DesiredLod = desiredLod;
            Priority = priority;
        }

        public DecorationPlacement Definition { get; }
        public ProceduralEnvironmentContext Environment { get; }
        public ProceduralVisualLod DesiredLod { get; }
        public GenerationPriority Priority { get; }
    }

    [Serializable]
    public sealed class ProceduralStyleProfile
    {
        [SerializeField, Range(0f, 1f)] private float _angularity = 0.82f;
        [SerializeField, Range(0f, 1f)] private float _asymmetry = 0.34f;
        [SerializeField, Range(0f, 1f)] private float _silhouetteVariation = 0.28f;
        [SerializeField, Range(0f, 1f)] private float _colorVariation = 0.12f;
        [SerializeField, Min(1)] private int _geometryVariantsPerKind = 4;
        [SerializeField, Min(1)] private int _styleVersion = 7;

        public float Angularity => _angularity;
        public float Asymmetry => _asymmetry;
        public float SilhouetteVariation => _silhouetteVariation;
        public float ColorVariation => _colorVariation;
        public int GeometryVariantsPerKind => _geometryVariantsPerKind;
        public int StyleVersion => _styleVersion;
    }

    [Serializable]
    public sealed class ProceduralGenerationBudget
    {
        [SerializeField, Min(0.1f)] private float _maxMillisecondsPerFrame = 2f;
        [SerializeField, Min(1)] private int _maxObjectsPerFrame = 24;
        [SerializeField, Min(3)] private int _maxVerticesPerFrame = 14000;
        [SerializeField, Min(1)] private int _lodChecksPerFrame = 16;

        public float MaxMillisecondsPerFrame => _maxMillisecondsPerFrame;
        public int MaxObjectsPerFrame => _maxObjectsPerFrame;
        public int MaxVerticesPerFrame => _maxVerticesPerFrame;
        public int LodChecksPerFrame => _lodChecksPerFrame;
    }

    public readonly struct ProceduralRuntimeMetrics
    {
        public ProceduralRuntimeMetrics(int activeObjects, int queueCount, int cachedMeshes, int generatedMeshes, int resolvedFiniteAssets,
            int cacheHits, int cacheMisses, int visibleVertices, int visibleTriangles, int estimatedDrawCalls, float lastFrameGenerationMilliseconds)
        {
            ActiveObjects = activeObjects; QueueCount = queueCount; CachedMeshes = cachedMeshes; GeneratedMeshes = generatedMeshes; ResolvedFiniteAssets = resolvedFiniteAssets;
            CacheHits = cacheHits; CacheMisses = cacheMisses; VisibleVertices = visibleVertices; VisibleTriangles = visibleTriangles;
            EstimatedDrawCalls = estimatedDrawCalls; LastFrameGenerationMilliseconds = lastFrameGenerationMilliseconds;
        }

        public int ActiveObjects { get; }
        public int QueueCount { get; }
        public int CachedMeshes { get; }
        public int GeneratedMeshes { get; }
        public int ResolvedFiniteAssets { get; }
        public int CacheHits { get; }
        public int CacheMisses { get; }
        public int VisibleVertices { get; }
        public int VisibleTriangles { get; }
        public int EstimatedDrawCalls { get; }
        public float LastFrameGenerationMilliseconds { get; }
    }
}
