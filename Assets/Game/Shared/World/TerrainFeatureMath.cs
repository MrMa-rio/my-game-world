using System;

namespace MyGameWorld.Shared.World
{
    public static class TerrainFeatureMath
    {
        public static float RadialInfluence(WorldElementBounds bounds, float x, float z, float power)
        {
            float dx = x - bounds.CenterX; float dz = z - bounds.CenterZ;
            float normalized = (float)Math.Sqrt((dx * dx) + (dz * dz)) / bounds.Radius;
            if (normalized >= 1f) return 0f;
            float smooth = 1f - (normalized * normalized * (3f - (2f * normalized)));
            return (float)Math.Pow(smooth, power);
        }

        public static float PathInfluence(PathDNA path, float x, float z)
        {
            float best = float.MaxValue;
            WorldVector3 previous = path.Start;
            const int segments = 16;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments; float inverse = 1f - t;
                WorldVector3 current = new WorldVector3(
                    (inverse * inverse * path.Start.X) + (2f * inverse * t * path.Control.X) + (t * t * path.End.X), 0f,
                    (inverse * inverse * path.Start.Z) + (2f * inverse * t * path.Control.Z) + (t * t * path.End.Z));
                best = Math.Min(best, DistanceToSegmentSquared(x, z, previous.X, previous.Z, current.X, current.Z));
                previous = current;
            }
            float distance = (float)Math.Sqrt(best);
            float outer = path.Width * 1.8f;
            if (distance >= outer) return 0f;
            float t2 = Math.Max(0f, Math.Min(1f, (distance - path.Width) / (outer - path.Width)));
            return 1f - (t2 * t2 * (3f - (2f * t2)));
        }

        private static float DistanceToSegmentSquared(float px, float pz, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax; float dz = bz - az; float length = (dx * dx) + (dz * dz);
            float t = length > 0f ? ((px - ax) * dx + (pz - az) * dz) / length : 0f;
            t = Math.Max(0f, Math.Min(1f, t)); float x = ax + t * dx; float z = az + t * dz;
            dx = px - x; dz = pz - z; return (dx * dx) + (dz * dz);
        }
    }
}
