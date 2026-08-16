using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.EntityRuntime
{
    public interface IPhysicalSurfaceProvider
    {
        int SurfaceId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class PhysicalSurfaceDescriptor : MonoBehaviour, IPhysicalSurfaceProvider
    {
        [SerializeField] private int _surfaceId;
        public int SurfaceId => _surfaceId;
        public void Configure(int surfaceId) => _surfaceId = surfaceId;
    }

    public readonly struct WorldEnvironmentSnapshot
    {
        public WorldEnvironmentSnapshot(int biomeId, int surfaceId, Vector3 windDirection, float windStrength, int weatherId)
        { BiomeId = biomeId; SurfaceId = surfaceId; WindDirection = windDirection; WindStrength = windStrength; WeatherId = weatherId; }
        public int BiomeId { get; }
        public int SurfaceId { get; }
        public Vector3 WindDirection { get; }
        public float WindStrength { get; }
        public int WeatherId { get; }
    }

    public interface IWorldEnvironmentContextProvider
    {
        WorldEnvironmentSnapshot Sample(Vector3 localPosition, GlobalPosition globalPosition);
    }
}
