using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class UnityLiquidBodyRuntime
    {
        private readonly Mesh _mesh;
        public UnityLiquidBodyRuntime(LiquidBodyDNA dna, Transform parent, Material material)
        {
            const int segments = 32;
            Root = new GameObject($"{dna.Substance} {dna.Form} {dna.ElementId.Value}");
            Root.transform.SetParent(parent, false);
            Root.transform.localPosition = new Vector3(dna.Bounds.CenterX, dna.SurfaceLevel, dna.Bounds.CenterZ);
            Root.AddComponent<WorldElementRuntimeIdentity>().Initialize(dna);
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            Color[] colors = new Color[vertices.Length];
            colors[0] = Color.white;
            for (int index = 0; index < segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                float ripple = 1f + Mathf.Sin(angle * 3f + (dna.Seed & 255) * 0.01f) * 0.035f;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * dna.RadiusX * ripple, 0f,
                    Mathf.Sin(angle) * dna.RadiusZ * ripple);
                colors[index + 1] = Color.white;
                int next = (index + 1) % segments;
                triangles[index * 3] = 0; triangles[index * 3 + 1] = next + 1; triangles[index * 3 + 2] = index + 1;
            }
            _mesh = new Mesh { name = $"Procedural {dna.Substance} {dna.Form}" };
            _mesh.vertices = vertices; _mesh.triangles = triangles; _mesh.colors = colors;
            _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
            Root.AddComponent<MeshFilter>().sharedMesh = _mesh;
            Root.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        public GameObject Root { get; }
        public void Dispose()
        {
            if (_mesh != null) Object.Destroy(_mesh);
            if (Root != null) Object.Destroy(Root);
        }
    }
}
