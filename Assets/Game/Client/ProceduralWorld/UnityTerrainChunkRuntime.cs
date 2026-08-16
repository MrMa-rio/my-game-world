using System;
using System.Collections.Generic;
using MyGameWorld.Shared.World;
using MyGameWorld.Client.EntityRuntime;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class UnityTerrainChunkRuntime : IDisposable
    {
        private readonly Mesh _terrainMesh;
        private readonly Mesh _wireMesh;
        private readonly GameObject _root;
        private readonly GameObject _wireframe;

        public UnityTerrainChunkRuntime(
            TerrainChunkData data,
            Transform parent,
            Material terrainMaterial,
            Material wireframeMaterial,
            TerrainSurfaceDNA terrainIdentity,
            int styleVersion)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _root = new GameObject($"Terrain Chunk {data.ChunkX},{data.ChunkZ}");
            _root.transform.SetParent(parent, false);
            _root.AddComponent<WorldElementRuntimeIdentity>().Initialize(terrainIdentity);
            MeshFilter filter = _root.AddComponent<MeshFilter>();
            MeshRenderer renderer = _root.AddComponent<MeshRenderer>();
            MeshCollider collider = _root.AddComponent<MeshCollider>();
            _terrainMesh = BuildTerrainMesh(data, styleVersion);
            filter.sharedMesh = _terrainMesh;
            renderer.sharedMaterial = terrainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            collider.sharedMesh = _terrainMesh;
            _root.AddComponent<PhysicalSurfaceDescriptor>().Configure((int)EnvironmentalSurfaceKind.Grass);

            _wireframe = new GameObject("Debug Wireframe");
            _wireframe.transform.SetParent(_root.transform, false);
            MeshFilter wireFilter = _wireframe.AddComponent<MeshFilter>();
            MeshRenderer wireRenderer = _wireframe.AddComponent<MeshRenderer>();
            _wireMesh = BuildWireMesh(data);
            wireFilter.sharedMesh = _wireMesh;
            wireRenderer.sharedMaterial = wireframeMaterial;
            wireRenderer.shadowCastingMode = ShadowCastingMode.Off;
            wireRenderer.receiveShadows = false;
            _wireframe.SetActive(false);
        }

        public void SetWireframeVisible(bool visible) => _wireframe.SetActive(visible);

        public void Dispose()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            if (_terrainMesh != null)
            {
                UnityEngine.Object.Destroy(_terrainMesh);
            }

            if (_wireMesh != null)
            {
                UnityEngine.Object.Destroy(_wireMesh);
            }
        }

        private static Mesh BuildTerrainMesh(TerrainChunkData data, int styleVersion)
        {
            Mesh mesh = new Mesh
            {
                name = $"Procedural Terrain {data.ChunkX},{data.ChunkZ}",
                indexFormat = data.VertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.vertices = ConvertVectors(data.Vertices);
            mesh.normals = ConvertVectors(data.Normals);
            mesh.colors = ProceduralTerrainSurfaceArtResolver.Resolve(data, styleVersion);
            mesh.triangles = data.Triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildWireMesh(TerrainChunkData data)
        {
            Mesh mesh = new Mesh
            {
                name = $"Terrain Wireframe {data.ChunkX},{data.ChunkZ}",
                indexFormat = data.VertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            Vector3[] vertices = ConvertVectors(data.Vertices);
            mesh.vertices = vertices;
            Color[] colors = new Color[vertices.Length];
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = Color.white;
            }

            mesh.colors = colors;
            HashSet<ulong> edges = new HashSet<ulong>();
            List<int> lines = new List<int>(data.Triangles.Length * 2);
            for (int index = 0; index < data.Triangles.Length; index += 3)
            {
                AddEdge(data.Triangles[index], data.Triangles[index + 1], edges, lines);
                AddEdge(data.Triangles[index + 1], data.Triangles[index + 2], edges, lines);
                AddEdge(data.Triangles[index + 2], data.Triangles[index], edges, lines);
            }

            mesh.SetIndices(lines, MeshTopology.Lines, 0, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddEdge(int first, int second, ISet<ulong> edges, ICollection<int> lines)
        {
            uint minimum = (uint)Math.Min(first, second);
            uint maximum = (uint)Math.Max(first, second);
            ulong key = ((ulong)minimum << 32) | maximum;
            if (edges.Add(key))
            {
                lines.Add(first);
                lines.Add(second);
            }
        }

        private static Vector3[] ConvertVectors(WorldVector3[] source)
        {
            Vector3[] result = new Vector3[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                WorldVector3 value = source[index];
                result[index] = new Vector3(value.X, value.Y, value.Z);
            }

            return result;
        }

        private static Color[] ConvertColors(WorldColor[] source)
        {
            Color[] result = new Color[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                WorldColor value = source[index];
                result[index] = new Color(value.Red, value.Green, value.Blue, 1f);
            }

            return result;
        }
    }
}
