using System;
using MyGameWorld.Shared.Core;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    public readonly struct AvatarEnvironmentContext : IEquatable<AvatarEnvironmentContext>
    {
        public AvatarEnvironmentContext(int biomeId, int surfaceId, float altitude, float slope)
        { BiomeId = biomeId; SurfaceId = surfaceId; Altitude = Mathf.Max(0f, altitude); Slope = Mathf.Clamp01(slope); }
        public int BiomeId { get; }
        public int SurfaceId { get; }
        public float Altitude { get; }
        public float Slope { get; }
        public bool Equals(AvatarEnvironmentContext other) => BiomeId == other.BiomeId && SurfaceId == other.SurfaceId
            && Altitude.Equals(other.Altitude) && Slope.Equals(other.Slope);
        public override bool Equals(object obj) => obj is AvatarEnvironmentContext other && Equals(other);
        public override int GetHashCode() => (((BiomeId * 397) ^ SurfaceId) * 397 ^ Altitude.GetHashCode()) * 397 ^ Slope.GetHashCode();
    }

    public enum AvatarSilhouetteFamily : byte
    { TemperateTraveler = 1, ForestRanger = 2, DesertWayfarer = 3, SnowHighlander = 4, RockyHighlander = 5 }

    public readonly struct AvatarStyleRecipe : IEquatable<AvatarStyleRecipe>
    {
        public AvatarStyleRecipe(AvatarSilhouetteFamily family, long appearanceSeed, Vector3 visualScale, Color colorTint,
            float angularity, float headScale, float torsoWidth, float hipWidth)
        {
            Family = family; AppearanceSeed = appearanceSeed; VisualScale = visualScale; ColorTint = colorTint;
            Angularity = Mathf.Clamp01(angularity); HeadScale = Mathf.Clamp(headScale, 0.85f, 1.2f);
            TorsoWidth = Mathf.Clamp(torsoWidth, 0.85f, 1.2f); HipWidth = Mathf.Clamp(hipWidth, 0.85f, 1.2f);
        }
        public AvatarSilhouetteFamily Family { get; }
        public long AppearanceSeed { get; }
        public Vector3 VisualScale { get; }
        public Color ColorTint { get; }
        public float Angularity { get; }
        public float HeadScale { get; }
        public float TorsoWidth { get; }
        public float HipWidth { get; }
        public bool Equals(AvatarStyleRecipe other) => Family == other.Family && VisualScale == other.VisualScale
            && AppearanceSeed == other.AppearanceSeed && ColorTint == other.ColorTint && Angularity.Equals(other.Angularity)
            && HeadScale.Equals(other.HeadScale) && TorsoWidth.Equals(other.TorsoWidth) && HipWidth.Equals(other.HipWidth);
        public override bool Equals(object obj) => obj is AvatarStyleRecipe other && Equals(other);
        public override int GetHashCode() => (((int)Family * 397) ^ VisualScale.GetHashCode()) * 397 ^ ColorTint.GetHashCode();
    }

    /// <summary>Client visual interpretation; it never changes identity, gameplay scale or collision.</summary>
    public static class AvatarEnvironmentalStyleResolver
    {
        public static AvatarStyleRecipe Resolve(long seed, AvatarEnvironmentContext context)
        {
            long appearanceSeed = DeriveSeed(seed, context);
            DeterministicRandom random = new DeterministicRandom(appearanceSeed);
            float heightVariation = Range(random, -0.045f, 0.045f);
            float widthVariation = Range(random, -0.035f, 0.035f);
            float altitude = Mathf.InverseLerp(0f, 90f, context.Altitude);
            switch (context.BiomeId)
            {
                case 2: return Recipe(AvatarSilhouetteFamily.ForestRanger, appearanceSeed, 0.91f + widthVariation,
                    1.08f + heightVariation, new Color(0.72f, 0.88f, 0.58f), 0.72f, 1.07f, 0.92f, 0.9f);
                case 3: return Recipe(AvatarSilhouetteFamily.DesertWayfarer, appearanceSeed, 0.88f + widthVariation,
                    1.11f + heightVariation, new Color(1f, 0.76f, 0.44f), 0.66f, 1.05f, 0.9f, 0.88f);
                case 4: return Recipe(AvatarSilhouetteFamily.SnowHighlander, appearanceSeed, 1.13f + widthVariation,
                    0.95f + heightVariation, new Color(0.62f, 0.8f, 0.98f), 0.82f, 1.1f, 1.14f, 1.1f);
                default:
                    if (context.SurfaceId == 3 || context.Slope > 0.32f || altitude > 0.62f)
                        return Recipe(AvatarSilhouetteFamily.RockyHighlander, appearanceSeed, 1.15f + widthVariation,
                            0.92f + heightVariation, new Color(0.67f, 0.7f, 0.58f), 0.88f, 1.12f, 1.16f, 1.12f);
                    return Recipe(AvatarSilhouetteFamily.TemperateTraveler, appearanceSeed, 0.98f + widthVariation,
                        1.04f + heightVariation, new Color(0.82f, 0.94f, 0.68f), 0.64f, 1.08f, 1.02f, 0.98f);
            }
        }

        private static AvatarStyleRecipe Recipe(AvatarSilhouetteFamily family, long appearanceSeed, float width, float height,
            Color tint, float angularity, float headScale, float torsoWidth, float hipWidth)
            => new AvatarStyleRecipe(family, appearanceSeed, new Vector3(width, height, width), tint, angularity,
                headScale, torsoWidth, hipWidth);
        private static float Range(DeterministicRandom random, float minimum, float maximum)
            => Mathf.Lerp(minimum, maximum, (float)random.NextUnitDouble());
        private static long DeriveSeed(long seed, AvatarEnvironmentContext context)
        {
            unchecked
            {
                ulong value = (ulong)seed;
                value ^= (uint)context.BiomeId * 0x9E3779B9UL;
                value ^= (uint)context.SurfaceId * 0x85EBCA6BUL;
                value ^= (uint)Mathf.RoundToInt(context.Altitude * 4f) * 0xC2B2AE35UL;
                value ^= (uint)Mathf.RoundToInt(context.Slope * 1024f) * 0x27D4EB2FUL;
                return (long)value;
            }
        }
    }
}
