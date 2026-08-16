using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class ProceduralRuntimeDebugInfo : MonoBehaviour
    {
        [SerializeField] private ProceduralVisualLod _lod;
        [SerializeField] private int _vertices;
        [SerializeField] private int _triangles;
        [SerializeField] private bool _cacheHit;
        [SerializeField] private bool _sharedMesh;
        [SerializeField] private float _generationMilliseconds;
        [SerializeField] private string _cacheKey;

        public ProceduralVisualLod Lod => _lod;
        public int Vertices => _vertices;
        public int Triangles => _triangles;
        public bool CacheHit => _cacheHit;
        public bool SharedMesh => _sharedMesh;
        public float GenerationMilliseconds => _generationMilliseconds;
        public string CacheKey => _cacheKey;

        public void UpdateInfo(ProceduralMeshResource resource, ProceduralVisualLod lod, bool cacheHit, float milliseconds)
        {
            _lod = lod; _vertices = resource.VertexCount; _triangles = resource.TriangleCount;
            _cacheHit = cacheHit; _sharedMesh = true; _generationMilliseconds = milliseconds; _cacheKey = resource.Key.ToString();
        }
    }
}
