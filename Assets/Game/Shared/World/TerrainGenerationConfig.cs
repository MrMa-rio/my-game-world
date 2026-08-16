using System;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainGenerationConfig
    {
        public TerrainGenerationConfig(
            float width,
            float depth,
            int requestedResolution,
            float maxHeight,
            int targetTriangleBudget,
            int chunkCountX,
            int chunkCountZ,
            TerrainShadingMode shadingMode)
        {
            if (width <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (depth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            if (requestedResolution < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedResolution));
            }

            if (maxHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeight));
            }

            if (targetTriangleBudget < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(targetTriangleBudget));
            }

            if (chunkCountX <= 0 || chunkCountZ <= 0 || chunkCountX != chunkCountZ)
            {
                throw new ArgumentException("V1 requires a positive square chunk grid.");
            }

            if (!Enum.IsDefined(typeof(TerrainShadingMode), shadingMode))
            {
                throw new ArgumentOutOfRangeException(nameof(shadingMode));
            }

            Width = width;
            Depth = depth;
            RequestedResolution = requestedResolution;
            MaxHeight = maxHeight;
            TargetTriangleBudget = targetTriangleBudget;
            ChunkCountX = chunkCountX;
            ChunkCountZ = chunkCountZ;
            ShadingMode = shadingMode;

            int maxCellsByBudget = (int)Math.Floor(Math.Sqrt(targetTriangleBudget / 2d));
            int requestedCells = requestedResolution - 1;
            int resolvedCells = Math.Min(maxCellsByBudget, requestedCells);
            resolvedCells -= resolvedCells % chunkCountX;
            if (resolvedCells < chunkCountX)
            {
                throw new ArgumentException("Triangle budget is too small for the requested chunk grid.");
            }

            ResolvedResolution = resolvedCells + 1;
            CellsPerChunk = resolvedCells / chunkCountX;
            TriangleCount = resolvedCells * resolvedCells * 2;
            LogicalVertexCount = ResolvedResolution * ResolvedResolution;
        }

        public float Width { get; }

        public float Depth { get; }

        public int RequestedResolution { get; }

        public int ResolvedResolution { get; }

        public float MaxHeight { get; }

        public int TargetTriangleBudget { get; }

        public int ChunkCountX { get; }

        public int ChunkCountZ { get; }

        public int CellsPerChunk { get; }

        public int TriangleCount { get; }

        public int LogicalVertexCount { get; }

        public TerrainShadingMode ShadingMode { get; }

        public static TerrainGenerationConfig CreateSandboxDefault()
        {
            return new TerrainGenerationConfig(
                width: 1000f,
                depth: 1000f,
                requestedResolution: 257,
                maxHeight: 40f,
                targetTriangleBudget: 80000,
                chunkCountX: 10,
                chunkCountZ: 10,
                shadingMode: TerrainShadingMode.Flat);
        }
    }
}
