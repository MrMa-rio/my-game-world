using System;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainChunkData
    {
        public TerrainChunkData(
            int chunkX,
            int chunkZ,
            WorldVector3[] vertices,
            WorldVector3[] normals,
            WorldColor[] colors,
            int[] triangles)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));

            if (Vertices.Length != Normals.Length || Vertices.Length != Colors.Length)
            {
                throw new ArgumentException("Vertex attributes must have matching lengths.");
            }

            if (Triangles.Length % 3 != 0)
            {
                throw new ArgumentException("Triangle indices must be divisible by three.");
            }
        }

        public int ChunkX { get; }

        public int ChunkZ { get; }

        public WorldVector3[] Vertices { get; }

        public WorldVector3[] Normals { get; }

        public WorldColor[] Colors { get; }

        public int[] Triangles { get; }

        public int VertexCount => Vertices.Length;

        public int TriangleCount => Triangles.Length / 3;
    }
}
