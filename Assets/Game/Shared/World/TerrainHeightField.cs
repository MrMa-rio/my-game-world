using System;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainHeightField
    {
        private readonly float[] _heights;
        private readonly float[] _pathMasks;
        private readonly WorldVector3[] _normals;

        public TerrainHeightField(
            int resolution,
            float width,
            float depth,
            float[] heights,
            float[] pathMasks)
        {
            if (resolution < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution));
            }

            int expectedCount = checked(resolution * resolution);
            if (heights == null || heights.Length != expectedCount)
            {
                throw new ArgumentException("Height count does not match resolution.", nameof(heights));
            }

            if (pathMasks == null || pathMasks.Length != expectedCount)
            {
                throw new ArgumentException("Path mask count does not match resolution.", nameof(pathMasks));
            }

            Resolution = resolution;
            Width = width;
            Depth = depth;
            _heights = (float[])heights.Clone();
            _pathMasks = (float[])pathMasks.Clone();
            _normals = BuildNormals();
        }

        public int Resolution { get; }

        public float Width { get; }

        public float Depth { get; }

        public float SpacingX => Width / (Resolution - 1);

        public float SpacingZ => Depth / (Resolution - 1);

        public float GetHeight(int x, int z) => _heights[GetIndex(x, z)];

        public float GetPathMask(int x, int z) => _pathMasks[GetIndex(x, z)];

        public WorldVector3 GetNormal(int x, int z) => _normals[GetIndex(x, z)];

        public float SampleHeight(float worldX, float worldZ)
        {
            return BilinearSample(_heights, worldX, worldZ);
        }

        public float SamplePathMask(float worldX, float worldZ)
        {
            return BilinearSample(_pathMasks, worldX, worldZ);
        }

        public WorldVector3 SampleNormal(float worldX, float worldZ)
        {
            float gridX = Clamp((worldX + (Width * 0.5f)) / SpacingX, 0f, Resolution - 1);
            float gridZ = Clamp((worldZ + (Depth * 0.5f)) / SpacingZ, 0f, Resolution - 1);
            int x = (int)Math.Round(gridX);
            int z = (int)Math.Round(gridZ);
            return GetNormal(x, z);
        }

        public float[] CopyHeights() => (float[])_heights.Clone();

        public float[] CopyPathMasks() => (float[])_pathMasks.Clone();

        private WorldVector3[] BuildNormals()
        {
            WorldVector3[] normals = new WorldVector3[_heights.Length];
            for (int z = 0; z < Resolution; z++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    int leftX = Math.Max(0, x - 1);
                    int rightX = Math.Min(Resolution - 1, x + 1);
                    int downZ = Math.Max(0, z - 1);
                    int upZ = Math.Min(Resolution - 1, z + 1);
                    float horizontalSpan = Math.Max(SpacingX, (rightX - leftX) * SpacingX);
                    float verticalSpan = Math.Max(SpacingZ, (upZ - downZ) * SpacingZ);
                    float gradientX = (GetHeight(rightX, z) - GetHeight(leftX, z)) / horizontalSpan;
                    float gradientZ = (GetHeight(x, upZ) - GetHeight(x, downZ)) / verticalSpan;
                    normals[GetIndex(x, z)] = Normalize(new WorldVector3(-gradientX, 1f, -gradientZ));
                }
            }

            return normals;
        }

        private float BilinearSample(float[] values, float worldX, float worldZ)
        {
            float gridX = Clamp((worldX + (Width * 0.5f)) / SpacingX, 0f, Resolution - 1);
            float gridZ = Clamp((worldZ + (Depth * 0.5f)) / SpacingZ, 0f, Resolution - 1);
            int x0 = (int)Math.Floor(gridX);
            int z0 = (int)Math.Floor(gridZ);
            int x1 = Math.Min(Resolution - 1, x0 + 1);
            int z1 = Math.Min(Resolution - 1, z0 + 1);
            float tx = gridX - x0;
            float tz = gridZ - z0;
            float bottom = Lerp(values[GetIndex(x0, z0)], values[GetIndex(x1, z0)], tx);
            float top = Lerp(values[GetIndex(x0, z1)], values[GetIndex(x1, z1)], tx);
            return Lerp(bottom, top, tz);
        }

        private int GetIndex(int x, int z)
        {
            if (x < 0 || x >= Resolution || z < 0 || z >= Resolution)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (z * Resolution) + x;
        }

        private static WorldVector3 Normalize(WorldVector3 value)
        {
            float length = (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y) + (value.Z * value.Z));
            return new WorldVector3(value.X / length, value.Y / length, value.Z / length);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float Lerp(float first, float second, float amount)
        {
            return first + ((second - first) * amount);
        }
    }
}
