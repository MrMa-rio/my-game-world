using System;

namespace MyGameWorld.Shared.World
{
    public static class GeologicalLandformModel
    {
        public const float MaximumHillSlopeDegrees = 34f;
        public const float MaximumDepressionSlopeDegrees = 18f;
        public const float MaximumDepressionDepthRatio = 0.12f;

        public static double Evaluate(LandformDNA feature, double worldX, double worldZ)
        {
            double dx = worldX - feature.Bounds.CenterX;
            double dz = worldZ - feature.Bounds.CenterZ;
            double radius = feature.Bounds.Radius;
            double normalizedRadius = Math.Sqrt(dx * dx + dz * dz) / radius;
            if (normalizedRadius >= 1d) return 0d;
            double inward = 1d - normalizedRadius;
            double smoothDome = inward * inward * (3d - 2d * inward);

            if (feature.ElementKind == WorldElementKind.Depression)
            {
                double slopeDepth = radius * Math.Tan(MaximumDepressionSlopeDegrees * Math.PI / 180d) * 0.36d;
                double ratioDepth = radius * MaximumDepressionDepthRatio;
                double depth = Math.Min(Math.Abs(feature.Amplitude), Math.Min(slopeDepth, ratioDepth));
                // A broad concave basin avoids needle-like pits. The asymmetric shallow spillway prevents
                // a perfectly circular crater silhouette while preserving point-sample determinism.
                double angle = ((uint)(feature.Seed ^ (feature.Seed >> 32)) / (double)uint.MaxValue) * Math.PI * 2d;
                double along = (dx * Math.Cos(angle) + dz * Math.Sin(angle)) / radius;
                double across = Math.Abs(-dx * Math.Sin(angle) + dz * Math.Cos(angle)) / radius;
                double spillway = along > 0d ? Smooth01(1d - across / 0.22d) * Smooth01(along) * 0.22d : 0d;
                return -depth * Math.Max(0d, smoothDome - spillway);
            }

            double slopeHeight = radius * Math.Tan(MaximumHillSlopeDegrees * Math.PI / 180d) * 0.48d;
            double height = Math.Min(Math.Max(0d, feature.Amplitude), slopeHeight);
            return height * smoothDome;
        }

        private static double Smooth01(double value)
        {
            double t = Math.Max(0d, Math.Min(1d, value));
            return t * t * (3d - 2d * t);
        }
    }
}
