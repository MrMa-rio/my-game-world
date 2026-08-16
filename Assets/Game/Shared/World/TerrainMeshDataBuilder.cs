using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainMeshDataBuilder
    {
        public TerrainChunkData BuildChunk(
            TerrainHeightField field,
            TerrainGenerationConfig config,
            BiomeDefinition biome,
            int chunkX,
            int chunkZ)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (biome == null)
            {
                throw new ArgumentNullException(nameof(biome));
            }

            if (chunkX < 0 || chunkX >= config.ChunkCountX || chunkZ < 0 || chunkZ >= config.ChunkCountZ)
            {
                throw new ArgumentOutOfRangeException("Chunk coordinate is outside the zone.");
            }

            return config.ShadingMode == TerrainShadingMode.Flat
                ? BuildFlat(field, config, biome, chunkX, chunkZ)
                : BuildSmooth(field, config, biome, chunkX, chunkZ);
        }

        private static TerrainChunkData BuildSmooth(
            TerrainHeightField field,
            TerrainGenerationConfig config,
            BiomeDefinition biome,
            int chunkX,
            int chunkZ)
        {
            int cells = config.CellsPerChunk;
            int localResolution = cells + 1;
            int vertexCount = localResolution * localResolution;
            WorldVector3[] vertices = new WorldVector3[vertexCount];
            WorldVector3[] normals = new WorldVector3[vertexCount];
            WorldColor[] colors = new WorldColor[vertexCount];
            int startX = chunkX * cells;
            int startZ = chunkZ * cells;

            for (int localZ = 0; localZ < localResolution; localZ++)
            {
                for (int localX = 0; localX < localResolution; localX++)
                {
                    int globalX = startX + localX;
                    int globalZ = startZ + localZ;
                    int index = (localZ * localResolution) + localX;
                    vertices[index] = CreateVertex(field, globalX, globalZ);
                    normals[index] = field.GetNormal(globalX, globalZ);
                    colors[index] = ResolveColor(field, config, biome, globalX, globalZ);
                }
            }

            int[] triangles = new int[cells * cells * 6];
            int triangleIndex = 0;
            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int bottomLeft = (z * localResolution) + x;
                    int topLeft = ((z + 1) * localResolution) + x;
                    int bottomRight = bottomLeft + 1;
                    int topRight = topLeft + 1;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                }
            }

            return new TerrainChunkData(chunkX, chunkZ, vertices, normals, colors, triangles);
        }

        private static TerrainChunkData BuildFlat(
            TerrainHeightField field,
            TerrainGenerationConfig config,
            BiomeDefinition biome,
            int chunkX,
            int chunkZ)
        {
            int cells = config.CellsPerChunk;
            int triangleCount = cells * cells * 2;
            List<WorldVector3> vertices = new List<WorldVector3>(triangleCount * 3);
            List<WorldVector3> normals = new List<WorldVector3>(triangleCount * 3);
            List<WorldColor> colors = new List<WorldColor>(triangleCount * 3);
            int[] triangles = new int[triangleCount * 3];
            int startX = chunkX * cells;
            int startZ = chunkZ * cells;

            for (int localZ = 0; localZ < cells; localZ++)
            {
                for (int localX = 0; localX < cells; localX++)
                {
                    int x = startX + localX;
                    int z = startZ + localZ;
                    AddFlatTriangle(field, config, biome, x, z, x, z + 1, x + 1, z, vertices, normals, colors, triangles);
                    AddFlatTriangle(field, config, biome, x + 1, z, x, z + 1, x + 1, z + 1, vertices, normals, colors, triangles);
                }
            }

            return new TerrainChunkData(
                chunkX,
                chunkZ,
                vertices.ToArray(),
                normals.ToArray(),
                colors.ToArray(),
                triangles);
        }

        private static void AddFlatTriangle(
            TerrainHeightField field,
            TerrainGenerationConfig config,
            BiomeDefinition biome,
            int ax,
            int az,
            int bx,
            int bz,
            int cx,
            int cz,
            List<WorldVector3> vertices,
            List<WorldVector3> normals,
            List<WorldColor> colors,
            int[] triangles)
        {
            WorldVector3 first = CreateVertex(field, ax, az);
            WorldVector3 second = CreateVertex(field, bx, bz);
            WorldVector3 third = CreateVertex(field, cx, cz);
            WorldVector3 normal = CalculateFaceNormal(first, second, third);
            int firstIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            colors.Add(ResolveColor(field, config, biome, ax, az));
            colors.Add(ResolveColor(field, config, biome, bx, bz));
            colors.Add(ResolveColor(field, config, biome, cx, cz));
            triangles[firstIndex] = firstIndex;
            triangles[firstIndex + 1] = firstIndex + 1;
            triangles[firstIndex + 2] = firstIndex + 2;
        }

        private static WorldVector3 CreateVertex(TerrainHeightField field, int x, int z)
        {
            return new WorldVector3(
                (-field.Width * 0.5f) + (x * field.SpacingX),
                field.GetHeight(x, z),
                (-field.Depth * 0.5f) + (z * field.SpacingZ));
        }

        private static WorldColor ResolveColor(
            TerrainHeightField field,
            TerrainGenerationConfig config,
            BiomeDefinition biome,
            int x,
            int z)
        {
            return biome.ResolveTerrainColor(
                field.GetHeight(x, z) / config.MaxHeight,
                field.GetNormal(x, z).Y,
                field.GetPathMask(x, z));
        }

        private static WorldVector3 CalculateFaceNormal(WorldVector3 first, WorldVector3 second, WorldVector3 third)
        {
            float abX = second.X - first.X;
            float abY = second.Y - first.Y;
            float abZ = second.Z - first.Z;
            float acX = third.X - first.X;
            float acY = third.Y - first.Y;
            float acZ = third.Z - first.Z;
            float x = (abY * acZ) - (abZ * acY);
            float y = (abZ * acX) - (abX * acZ);
            float z = (abX * acY) - (abY * acX);
            float length = (float)Math.Sqrt((x * x) + (y * y) + (z * z));
            return new WorldVector3(x / length, y / length, z / length);
        }
    }
}
