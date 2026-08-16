using System;
using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.EntityRuntime
{
    public readonly struct WorldSpatialContext
    {
        public WorldSpatialContext(WorldCellCoordinate? cell, long? regionId, BiomeId? biome, ushort? surfaceId)
        {
            if (regionId.HasValue && regionId.Value <= 0L) throw new ArgumentOutOfRangeException(nameof(regionId));
            Cell = cell; RegionId = regionId; Biome = biome; SurfaceId = surfaceId;
        }

        public WorldCellCoordinate? Cell { get; }
        public long? RegionId { get; }
        public BiomeId? Biome { get; }
        public ushort? SurfaceId { get; }
        public bool HasCell => Cell.HasValue;
    }

    public readonly struct WorldCoordinateFrame
    {
        public WorldCoordinateFrame(GlobalPosition origin) { Origin = origin; }
        public GlobalPosition Origin { get; }

        public Vector3 ToLocal(GlobalPosition global) => new Vector3(
            (float)(global.X - Origin.X), (float)(global.Y - Origin.Y), (float)(global.Z - Origin.Z));

        public GlobalPosition ToGlobal(Vector3 local) => Origin.Add(local.x, local.y, local.z);
    }

    public sealed class WorldPresence
    {
        private readonly Transform _transform;
        private WorldCoordinateFrame _coordinateFrame;

        public WorldPresence(Transform transform, GlobalPosition globalPosition, WorldCoordinateFrame coordinateFrame,
            WorldSpatialContext spatialContext = default)
        {
            _transform = transform != null ? transform : throw new ArgumentNullException(nameof(transform));
            GlobalPosition = globalPosition;
            _coordinateFrame = coordinateFrame;
            SpatialContext = spatialContext;
            SynchronizeLocalPosition();
        }

        public GlobalPosition GlobalPosition { get; private set; }
        public Vector3 LocalPosition => _transform.position;
        public WorldCoordinateFrame CoordinateFrame => _coordinateFrame;
        public WorldSpatialContext SpatialContext { get; private set; }

        public void SetGlobalPosition(GlobalPosition position)
        {
            GlobalPosition = position;
            SynchronizeLocalPosition();
        }

        public void SetLocalPosition(Vector3 position)
        {
            _transform.position = position;
            GlobalPosition = _coordinateFrame.ToGlobal(position);
        }

        public void ApplyCoordinateFrame(WorldCoordinateFrame coordinateFrame)
        {
            _coordinateFrame = coordinateFrame;
            SynchronizeLocalPosition();
        }

        public void UpdateSpatialContext(WorldSpatialContext context) => SpatialContext = context;

        private void SynchronizeLocalPosition() => _transform.position = _coordinateFrame.ToLocal(GlobalPosition);
    }
}
