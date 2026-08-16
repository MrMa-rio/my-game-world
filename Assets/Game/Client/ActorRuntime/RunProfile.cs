using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Run Profile")]
    public sealed class RunProfile : ScriptableObject
    {
        [SerializeField, Min(1f)] private float _speedMultiplier = 1.8f;
        public float SpeedMultiplier => Mathf.Max(1f, _speedMultiplier);
    }
}
