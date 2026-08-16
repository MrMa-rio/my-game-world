using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Walk Profile")]
    public sealed class WalkProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _speed = 4f;

        public float Speed => _speed;
    }
}
