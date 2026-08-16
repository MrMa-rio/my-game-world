using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Jump Profile")]
    public sealed class JumpProfile : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _verticalSpeed = 7.5f;
        [SerializeField, Min(0f)] private float _cooldown = 0.2f;

        public float VerticalSpeed => _verticalSpeed;
        public float Cooldown => _cooldown;
    }
}
