using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public sealed class DecorationPlacement : WorldElementDNA, IEquatable<DecorationPlacement>
    {
        public DecorationPlacement(
            WorldElementId elementId,
            ZoneDNA zone,
            long seed,
            DecorationKind kind,
            AssetId visualAssetId,
            WorldVector3 position,
            float yawDegrees,
            float scale,
            float shapeA = 1f,
            float shapeB = 1f,
            float shapeC = 1f)
            : base(elementId, zone.ZoneId, ToElementKind(kind), seed, zone.GeneratorVersion, zone.AssetCatalogVersion,
                new WorldElementBounds(position.X, position.Z, Math.Max(0.5f, scale * 2f)))
        {
            if (!Enum.IsDefined(typeof(DecorationKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }

            if (visualAssetId.Value == 0)
            {
                throw new ArgumentException("A visual AssetId is required.", nameof(visualAssetId));
            }

            Kind = kind;
            VisualAssetId = visualAssetId;
            Position = position;
            YawDegrees = yawDegrees;
            Scale = scale;
            ShapeA = shapeA;
            ShapeB = shapeB;
            ShapeC = shapeC;
        }

        public long StableId => ElementId.Value;

        public DecorationKind Kind { get; }
        public AssetId VisualAssetId { get; }

        public WorldVector3 Position { get; }

        public float YawDegrees { get; }

        public float Scale { get; }
        public float ShapeA { get; }
        public float ShapeB { get; }
        public float ShapeC { get; }

        public bool Equals(DecorationPlacement other)
        {
            return StableId == other.StableId
                && Seed == other.Seed
                && Kind == other.Kind
                && VisualAssetId == other.VisualAssetId
                && Position.Equals(other.Position)
                && YawDegrees.Equals(other.YawDegrees)
                && Scale.Equals(other.Scale)
                && ShapeA.Equals(other.ShapeA)
                && ShapeB.Equals(other.ShapeB)
                && ShapeC.Equals(other.ShapeC);
        }

        public override bool Equals(object obj) => obj is DecorationPlacement other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StableId.GetHashCode();
                hash = (hash * 397) ^ Seed.GetHashCode();
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ VisualAssetId.GetHashCode();
                hash = (hash * 397) ^ Position.GetHashCode();
                hash = (hash * 397) ^ YawDegrees.GetHashCode();
                hash = (hash * 397) ^ Scale.GetHashCode();
                hash = (hash * 397) ^ ShapeA.GetHashCode();
                hash = (hash * 397) ^ ShapeB.GetHashCode();
                return (hash * 397) ^ ShapeC.GetHashCode();
            }
        }

        private static WorldElementKind ToElementKind(DecorationKind kind)
        {
            switch (kind)
            {
                case DecorationKind.Tree: return WorldElementKind.Tree;
                case DecorationKind.Rock: return WorldElementKind.Rock;
                case DecorationKind.Bush: return WorldElementKind.Bush;
                case DecorationKind.ScaleMarker: return WorldElementKind.ScaleMarker;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
