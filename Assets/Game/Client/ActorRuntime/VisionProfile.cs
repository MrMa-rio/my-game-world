using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Vision Profile")]
    public sealed class VisionProfile : ScriptableObject
    {
        [SerializeField, Range(1f, 360f)] private float _fieldOfView = 120f;
        [SerializeField, Min(0.1f)] private float _range = 60f;
        [SerializeField, Min(0f)] private float _eyeHeight = 1.65f;
        [SerializeField] private LayerMask _visibleLayers = ~0;
        [SerializeField] private LayerMask _occlusionLayers = ~0;
        [SerializeField, Range(8, 256)] private int _candidateCapacity = 64;

        public float FieldOfView => _fieldOfView;
        public float Range => _range;
        public float EyeHeight => _eyeHeight;
        public int VisibleLayers => _visibleLayers;
        public int OcclusionLayers => _occlusionLayers;
        public int CandidateCapacity => _candidateCapacity;
    }
}
