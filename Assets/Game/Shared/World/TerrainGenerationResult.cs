using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainGenerationResult
    {
        private readonly TerrainChunkData[] _chunks;

        public TerrainGenerationResult(
            TerrainHeightField heightField,
            TerrainGenerationConfig config,
            IReadOnlyList<TerrainChunkData> chunks,
            ulong fingerprint)
        {
            HeightField = heightField ?? throw new ArgumentNullException(nameof(heightField));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            _chunks = new TerrainChunkData[chunks.Count];
            int vertices = 0;
            int triangles = 0;
            for (int index = 0; index < chunks.Count; index++)
            {
                _chunks[index] = chunks[index];
                vertices += chunks[index].VertexCount;
                triangles += chunks[index].TriangleCount;
            }

            RenderedVertexCount = vertices;
            TriangleCount = triangles;
            Fingerprint = fingerprint;
        }

        public TerrainHeightField HeightField { get; }

        public TerrainGenerationConfig Config { get; }

        public IReadOnlyList<TerrainChunkData> Chunks => _chunks;

        public int LogicalVertexCount => Config.LogicalVertexCount;

        public int RenderedVertexCount { get; }

        public int TriangleCount { get; }

        public ulong Fingerprint { get; }
    }
}
