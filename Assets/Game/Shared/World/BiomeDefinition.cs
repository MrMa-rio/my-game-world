using System;

namespace MyGameWorld.Shared.World
{
    public sealed class BiomeDefinition
    {
        public BiomeDefinition(
            BiomeId id,
            float baseHeight,
            float macroScale,
            float macroAmplitude,
            int macroOctaves,
            float detailScale,
            float detailAmplitude,
            int detailOctaves,
            float pathScale,
            float pathHalfWidth,
            float rockSlopeThreshold,
            float decorationDensity,
            float minimumDecorationDistance,
            WorldColor lowGrassColor,
            WorldColor highGrassColor,
            WorldColor dirtColor,
            WorldColor rockColor)
        {
            if (!Enum.IsDefined(typeof(BiomeId), id))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (macroScale <= 0f || detailScale <= 0f || pathScale <= 0f)
            {
                throw new ArgumentException("Noise scales must be positive.");
            }

            if (macroAmplitude < 0f || detailAmplitude < 0f)
            {
                throw new ArgumentException("Noise amplitudes cannot be negative.");
            }

            if (macroOctaves <= 0 || detailOctaves <= 0)
            {
                throw new ArgumentException("Noise octave counts must be positive.");
            }

            if (pathHalfWidth <= 0f || rockSlopeThreshold <= 0f || rockSlopeThreshold >= 1f)
            {
                throw new ArgumentException("Biome thresholds are invalid.");
            }

            if (decorationDensity < 0f || minimumDecorationDistance <= 0f)
            {
                throw new ArgumentException("Decoration parameters are invalid.");
            }

            Id = id;
            BaseHeight = baseHeight;
            MacroScale = macroScale;
            MacroAmplitude = macroAmplitude;
            MacroOctaves = macroOctaves;
            DetailScale = detailScale;
            DetailAmplitude = detailAmplitude;
            DetailOctaves = detailOctaves;
            PathScale = pathScale;
            PathHalfWidth = pathHalfWidth;
            RockSlopeThreshold = rockSlopeThreshold;
            DecorationDensity = decorationDensity;
            MinimumDecorationDistance = minimumDecorationDistance;
            LowGrassColor = lowGrassColor;
            HighGrassColor = highGrassColor;
            DirtColor = dirtColor;
            RockColor = rockColor;
        }

        public BiomeId Id { get; }

        public float BaseHeight { get; }

        public float MacroScale { get; }

        public float MacroAmplitude { get; }

        public int MacroOctaves { get; }

        public float DetailScale { get; }

        public float DetailAmplitude { get; }

        public int DetailOctaves { get; }

        public float PathScale { get; }

        public float PathHalfWidth { get; }

        public float RockSlopeThreshold { get; }

        public float DecorationDensity { get; }

        public float MinimumDecorationDistance { get; }

        public WorldColor LowGrassColor { get; }

        public WorldColor HighGrassColor { get; }

        public WorldColor DirtColor { get; }

        public WorldColor RockColor { get; }

        public WorldColor ResolveTerrainColor(float normalizedHeight, float normalY, float pathMask)
        {
            float slope = 1f - Clamp01(normalY);
            WorldColor grass = WorldColor.Lerp(LowGrassColor, HighGrassColor, normalizedHeight);
            WorldColor slopeColor = slope >= RockSlopeThreshold
                ? WorldColor.Lerp(grass, RockColor, SmoothStep(RockSlopeThreshold, 0.65f, slope))
                : grass;
            return WorldColor.Lerp(slopeColor, DirtColor, Clamp01(pathMask * 1.35f));
        }

        public static BiomeDefinition CreateTemperateGrassland()
        {
            return new BiomeDefinition(
                BiomeId.TemperateGrassland,
                baseHeight: 5f,
                macroScale: 42f,
                macroAmplitude: 7.2f,
                macroOctaves: 4,
                detailScale: 10f,
                detailAmplitude: 0.85f,
                detailOctaves: 2,
                pathScale: 30f,
                pathHalfWidth: 0.085f,
                rockSlopeThreshold: 0.22f,
                decorationDensity: 0.0065f,
                minimumDecorationDistance: 5.5f,
                lowGrassColor: new WorldColor(0.22f, 0.55f, 0.24f),
                highGrassColor: new WorldColor(0.48f, 0.72f, 0.30f),
                dirtColor: new WorldColor(0.52f, 0.35f, 0.19f),
                rockColor: new WorldColor(0.39f, 0.42f, 0.38f));
        }

        public static BiomeDefinition CreateExpandedTemperateGrassland()
        {
            return new BiomeDefinition(
                BiomeId.TemperateGrassland,
                baseHeight: 12f,
                macroScale: 48f,
                macroAmplitude: 8.6f,
                macroOctaves: 4,
                detailScale: 12f,
                detailAmplitude: 1.05f,
                detailOctaves: 2,
                pathScale: 36f,
                pathHalfWidth: 0.085f,
                rockSlopeThreshold: 0.19f,
                decorationDensity: 0.0015f,
                minimumDecorationDistance: 7f,
                lowGrassColor: new WorldColor(0.19f, 0.48f, 0.22f),
                highGrassColor: new WorldColor(0.52f, 0.74f, 0.31f),
                dirtColor: new WorldColor(0.50f, 0.32f, 0.17f),
                rockColor: new WorldColor(0.37f, 0.40f, 0.39f));
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));

        private static float SmoothStep(float minimum, float maximum, float value)
        {
            float t = Clamp01((value - minimum) / (maximum - minimum));
            return t * t * (3f - (2f * t));
        }
    }
}
