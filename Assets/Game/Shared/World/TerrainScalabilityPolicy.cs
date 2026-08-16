using System;

namespace MyGameWorld.Shared.World
{
    public readonly struct TerrainScaleAssessment
    {
        public TerrainScaleAssessment(bool isSupported, string reason, float sampleSpacing, float verticalToHorizontalRatio)
        { IsSupported = isSupported; Reason = reason; SampleSpacing = sampleSpacing; VerticalToHorizontalRatio = verticalToHorizontalRatio; }
        public bool IsSupported { get; }
        public string Reason { get; }
        public float SampleSpacing { get; }
        public float VerticalToHorizontalRatio { get; }
    }

    public static class TerrainScalabilityPolicy
    {
        // The detailed compatibility terrain is deliberately bounded. More visible area belongs to the hierarchy,
        // never to a larger eager heightfield with proportionally more physics and decoration work.
        public const float MaximumDetailedSpan = 5000f;
        public const float MaximumDetailedHeight = 500f;
        public const float MaximumSampleSpacing = 16f;

        public static TerrainScaleAssessment AssessDetailed(TerrainGenerationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            float spacing = Math.Max(config.Width, config.Depth) / (config.ResolvedResolution - 1);
            float ratio = config.MaxHeight / Math.Min(config.Width, config.Depth);
            if (config.Width > MaximumDetailedSpan || config.Depth > MaximumDetailedSpan)
                return new TerrainScaleAssessment(false, "Detailed terrain exceeds 5 km. Extend visibility with hierarchical cells.", spacing, ratio);
            if (config.MaxHeight > MaximumDetailedHeight)
                return new TerrainScaleAssessment(false, "Detailed terrain height exceeds the validated vertical envelope.", spacing, ratio);
            if (spacing > MaximumSampleSpacing)
                return new TerrainScaleAssessment(false, "Detailed height samples are too sparse for stable slopes and decoration placement.", spacing, ratio);
            return new TerrainScaleAssessment(true, string.Empty, spacing, ratio);
        }

        public static void EnsureDetailedSupported(TerrainGenerationConfig config)
        {
            TerrainScaleAssessment assessment = AssessDetailed(config);
            if (!assessment.IsSupported) throw new InvalidOperationException(assessment.Reason);
        }
    }
}
