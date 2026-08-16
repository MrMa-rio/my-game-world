using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Physical Body Profile")]
    public sealed class PhysicalBodyProfile : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _mass = 75f;
        [SerializeField, Min(0f)] private float _softResistance = 0.15f;
        [SerializeField] private Vector3 _center = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.1f)] private float _height = 2f;
        [SerializeField, Min(0.05f)] private float _radius = 0.4f;

        public float Mass => _mass;
        public float SoftResistance => Mathf.Clamp01(_softResistance);
        public Vector3 Center => _center;
        public float Height => Mathf.Max(_height, _radius * 2f);
        public float Radius => Mathf.Min(_radius, Height * 0.5f);
    }
}
