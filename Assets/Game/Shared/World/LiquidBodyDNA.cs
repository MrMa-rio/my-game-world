using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public enum LiquidSubstance : byte { Water = 1, Lava = 2 }
    public enum LiquidQuantityTier : byte { Trace = 1, Small = 2, Medium = 3, Large = 4, Vast = 5 }
    public enum LiquidBodyForm : byte { Puddle = 1, Pond = 2, Lake = 3, Sea = 4, Stream = 5, River = 6 }

    public sealed class LiquidBodyDNA : WorldElementDNA
    {
        public LiquidBodyDNA(WorldElementId id, ZoneDNA zone, long seed, LiquidSubstance substance,
            float volume, float surfaceLevel, float radiusX, float radiusZ, float flowRate,
            WorldElementBounds bounds)
            : base(id, zone.ZoneId, WorldElementKind.LiquidBody, seed, zone.GeneratorVersion, zone.AssetCatalogVersion, bounds)
        {
            if (!Enum.IsDefined(typeof(LiquidSubstance), substance)) throw new ArgumentOutOfRangeException(nameof(substance));
            if (volume <= 0f || radiusX <= 0f || radiusZ <= 0f) throw new ArgumentOutOfRangeException(nameof(volume));
            if (flowRate < 0f) throw new ArgumentOutOfRangeException(nameof(flowRate));
            Substance = substance; Volume = volume; SurfaceLevel = surfaceLevel;
            VisualAssetId = WorldVisualAssetIds.ForLiquid(substance);
            RadiusX = radiusX; RadiusZ = radiusZ; FlowRate = flowRate;
            QuantityTier = LiquidBodyClassifier.ResolveQuantity(volume);
            Form = LiquidBodyClassifier.ResolveForm(volume, flowRate, radiusX, radiusZ);
        }

        public LiquidSubstance Substance { get; }
        public AssetId VisualAssetId { get; }
        public float Volume { get; }
        public float SurfaceLevel { get; }
        public float RadiusX { get; }
        public float RadiusZ { get; }
        public float FlowRate { get; }
        public LiquidQuantityTier QuantityTier { get; }
        public LiquidBodyForm Form { get; }
    }

    public static class LiquidBodyClassifier
    {
        public static LiquidQuantityTier ResolveQuantity(float volume)
        {
            if (volume <= 0f) throw new ArgumentOutOfRangeException(nameof(volume));
            if (volume < 25f) return LiquidQuantityTier.Trace;
            if (volume < 500f) return LiquidQuantityTier.Small;
            if (volume < 15000f) return LiquidQuantityTier.Medium;
            if (volume < 250000f) return LiquidQuantityTier.Large;
            return LiquidQuantityTier.Vast;
        }

        public static LiquidBodyForm ResolveForm(float volume, float flowRate, float radiusX, float radiusZ)
        {
            if (volume <= 0f || flowRate < 0f || radiusX <= 0f || radiusZ <= 0f) throw new ArgumentOutOfRangeException();
            float aspect = Math.Max(radiusX, radiusZ) / Math.Min(radiusX, radiusZ);
            bool flowing = flowRate >= 0.2f || aspect >= 4f;
            if (flowing) return volume < 15000f ? LiquidBodyForm.Stream : LiquidBodyForm.River;
            if (volume < 25f) return LiquidBodyForm.Puddle;
            if (volume < 500f) return LiquidBodyForm.Pond;
            if (volume < 250000f) return LiquidBodyForm.Lake;
            return LiquidBodyForm.Sea;
        }
    }
}
