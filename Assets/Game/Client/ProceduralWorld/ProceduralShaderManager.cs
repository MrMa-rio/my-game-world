using System;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum ProceduralShaderQuality : byte { Low = 1, Medium = 2, High = 3, Ultra = 4 }

    [Flags]
    public enum ProceduralShaderLayer : byte
    {
        BaseLighting = 1 << 0,
        ToonBands = 1 << 1,
        DynamicShadows = 1 << 2,
        RimLighting = 1 << 3,
        StylizedReflection = 1 << 4
    }

    public readonly struct ProceduralShaderBudget
    {
        public ProceduralShaderBudget(ProceduralShaderLayer layers, int diffuseBands, int shadowBands,
            float reflectionStrength, float rimStrength)
        { Layers = layers; DiffuseBands = diffuseBands; ShadowBands = shadowBands; ReflectionStrength = reflectionStrength; RimStrength = rimStrength; }
        public ProceduralShaderLayer Layers { get; }
        public int DiffuseBands { get; }
        public int ShadowBands { get; }
        public float ReflectionStrength { get; }
        public float RimStrength { get; }
        public bool Has(ProceduralShaderLayer layer) => (Layers & layer) != 0;
    }

    public readonly struct ProceduralLightingPaletteSample
    {
        public ProceduralLightingPaletteSample(Color worldTint, Color reflectionColor, Color rimColor, Color shadowColor, float exposure)
        { WorldTint = worldTint; ReflectionColor = reflectionColor; RimColor = rimColor; ShadowColor = shadowColor; Exposure = exposure; }
        public Color WorldTint { get; }
        public Color ReflectionColor { get; }
        public Color RimColor { get; }
        public Color ShadowColor { get; }
        public float Exposure { get; }
    }

    [Serializable]
    public sealed class ProceduralShaderProfile
    {
        [SerializeField] private ProceduralShaderQuality _quality = ProceduralShaderQuality.High;
        public ProceduralShaderQuality Quality { get => _quality; set => _quality = value; }
    }

    public sealed class ProceduralShaderManager : IDisposable
    {
        private static readonly int LayerFlags = Shader.PropertyToID("_ProceduralShaderLayers");
        private static readonly int LightingParameters = Shader.PropertyToID("_ProceduralLightingParameters");
        private static readonly int ReflectionColor = Shader.PropertyToID("_ProceduralReflectionColor");
        private static readonly int ShadowParameters = Shader.PropertyToID("_ProceduralShadowParameters");
        private static readonly int ShadowColor = Shader.PropertyToID("_ProceduralShadowColor");
        private static readonly int WorldTimeTint = Shader.PropertyToID("_WorldTimeTint");
        private static readonly int WorldTimeRimColor = Shader.PropertyToID("_WorldTimeRimColor");
        private readonly ProceduralShaderProfile _profile;

        public ProceduralShaderManager(ProceduralShaderProfile profile) { _profile = profile ?? new ProceduralShaderProfile(); ApplyDefaults(); }
        public ProceduralShaderQuality Quality => _profile.Quality;
        public ProceduralShaderBudget Budget => ResolveBudget(_profile.Quality);

        public void CycleQuality()
        {
            _profile.Quality = _profile.Quality == ProceduralShaderQuality.Ultra
                ? ProceduralShaderQuality.Low : (ProceduralShaderQuality)((int)_profile.Quality + 1);
        }

        public void Apply(WorldTimeSnapshot time, Light sun, Light moon)
        {
            ProceduralShaderBudget budget = Budget;
            float sunElevation = sun != null ? Mathf.Clamp01(-sun.transform.forward.y) : 0f;
            float moonElevation = moon != null ? Mathf.Clamp01(-moon.transform.forward.y) : 0f;
            float dayShadow = Mathf.Lerp(0.34f, 0.84f, Mathf.SmoothStep(0f, 1f, sunElevation));
            float nightShadow = Mathf.Lerp(0.16f, 0.46f, Mathf.SmoothStep(0f, 1f, moonElevation));
            float shadowStrength = Mathf.Lerp(nightShadow, dayShadow, time.Daylight);
            bool dynamicShadows = budget.Has(ProceduralShaderLayer.DynamicShadows);
            if (!dynamicShadows) shadowStrength = 0f;
            if (sun != null) { sun.shadows = dynamicShadows ? LightShadows.Soft : LightShadows.None; sun.shadowStrength = shadowStrength; }
            if (moon != null) { moon.shadows = dynamicShadows ? LightShadows.Soft : LightShadows.None; moon.shadowStrength = shadowStrength * 0.72f; }

            float toon = budget.Has(ProceduralShaderLayer.ToonBands) ? 1f : 0f;
            float reflection = budget.Has(ProceduralShaderLayer.StylizedReflection) ? budget.ReflectionStrength : 0f;
            float rim = budget.Has(ProceduralShaderLayer.RimLighting) ? budget.RimStrength : 0f;
            Shader.SetGlobalVector(LayerFlags, new Vector4(toon, reflection, rim, dynamicShadows ? 1f : 0f));
            float bandSoftness = _profile.Quality == ProceduralShaderQuality.Low ? 0.18f :
                _profile.Quality == ProceduralShaderQuality.Medium ? 0.26f : _profile.Quality == ProceduralShaderQuality.High ? 0.34f : 0.42f;
            Shader.SetGlobalVector(LightingParameters, new Vector4(budget.DiffuseBands, budget.ShadowBands, reflection, bandSoftness));
            ProceduralLightingPaletteSample palette = EvaluatePalette(time.Hour);
            Shader.SetGlobalColor(ReflectionColor, palette.ReflectionColor);
            Shader.SetGlobalColor(ShadowColor, palette.ShadowColor);
            Shader.SetGlobalVector(WorldTimeTint, new Vector4(palette.WorldTint.r, palette.WorldTint.g, palette.WorldTint.b, palette.Exposure));
            Shader.SetGlobalVector(WorldTimeRimColor, new Vector4(palette.RimColor.r, palette.RimColor.g, palette.RimColor.b, rim));
            Shader.SetGlobalVector(ShadowParameters, new Vector4(shadowStrength, sunElevation, moonElevation, time.Daylight));
        }

        public static ProceduralLightingPaletteSample EvaluatePalette(float hour)
        {
            float h = Mathf.Repeat(hour, 24f);
            PaletteKey[] keys = PaletteKeys;
            for (int index = 0; index < keys.Length; index++)
            {
                PaletteKey current = keys[index]; PaletteKey next = keys[(index + 1) % keys.Length];
                float end = index == keys.Length - 1 ? next.Hour + 24f : next.Hour;
                float sample = index == keys.Length - 1 && h < current.Hour ? h + 24f : h;
                if (sample < current.Hour || sample > end) continue;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(current.Hour, end, sample));
                return new ProceduralLightingPaletteSample(Color.Lerp(current.WorldTint, next.WorldTint, t),
                    Color.Lerp(current.Reflection, next.Reflection, t), Color.Lerp(current.Rim, next.Rim, t),
                    Color.Lerp(current.Shadow, next.Shadow, t), Mathf.Lerp(current.Exposure, next.Exposure, t));
            }
            return PaletteKeys[0].ToSample();
        }

        public static ProceduralShaderBudget ResolveBudget(ProceduralShaderQuality quality)
        {
            switch (quality)
            {
                case ProceduralShaderQuality.Low:
                    return new ProceduralShaderBudget(ProceduralShaderLayer.BaseLighting | ProceduralShaderLayer.ToonBands, 2, 1, 0f, 0f);
                case ProceduralShaderQuality.Medium:
                    return new ProceduralShaderBudget(ProceduralShaderLayer.BaseLighting | ProceduralShaderLayer.ToonBands |
                        ProceduralShaderLayer.DynamicShadows | ProceduralShaderLayer.RimLighting, 3, 2, 0f, 0.55f);
                case ProceduralShaderQuality.Ultra:
                    return new ProceduralShaderBudget(ProceduralShaderLayer.BaseLighting | ProceduralShaderLayer.ToonBands |
                        ProceduralShaderLayer.DynamicShadows | ProceduralShaderLayer.RimLighting | ProceduralShaderLayer.StylizedReflection, 5, 4, 1f, 1f);
                default:
                    return new ProceduralShaderBudget(ProceduralShaderLayer.BaseLighting | ProceduralShaderLayer.ToonBands |
                        ProceduralShaderLayer.DynamicShadows | ProceduralShaderLayer.RimLighting | ProceduralShaderLayer.StylizedReflection, 4, 3, 0.72f, 0.78f);
            }
        }

        public void Dispose() => ApplyDefaults();
        private static void ApplyDefaults()
        {
            Shader.SetGlobalVector(LayerFlags, new Vector4(1f, 0f, 0f, 1f));
            Shader.SetGlobalVector(LightingParameters, new Vector4(3f, 2f, 0f, 0f));
            Shader.SetGlobalColor(ReflectionColor, Color.white);
            Shader.SetGlobalColor(ShadowColor, new Color(0.25f, 0.32f, 0.48f));
            Shader.SetGlobalVector(ShadowParameters, new Vector4(0.6f, 1f, 0f, 1f));
        }

        private readonly struct PaletteKey
        {
            public PaletteKey(float hour, Color worldTint, Color reflection, Color rim, Color shadow, float exposure)
            { Hour = hour; WorldTint = worldTint; Reflection = reflection; Rim = rim; Shadow = shadow; Exposure = exposure; }
            public float Hour { get; } public Color WorldTint { get; } public Color Reflection { get; }
            public Color Rim { get; } public Color Shadow { get; } public float Exposure { get; }
            public ProceduralLightingPaletteSample ToSample() => new ProceduralLightingPaletteSample(WorldTint, Reflection, Rim, Shadow, Exposure);
        }

        private static readonly PaletteKey[] PaletteKeys =
        {
            new PaletteKey(0f, new Color(0.36f,0.46f,0.78f), new Color(0.12f,0.25f,0.66f), new Color(0.16f,0.38f,0.92f), new Color(0.08f,0.13f,0.32f), 0.64f),
            new PaletteKey(4f, new Color(0.43f,0.48f,0.82f), new Color(0.20f,0.28f,0.72f), new Color(0.40f,0.34f,0.94f), new Color(0.12f,0.14f,0.38f), 0.67f),
            new PaletteKey(5.5f, new Color(0.68f,0.58f,0.82f), new Color(0.52f,0.38f,0.82f), new Color(0.72f,0.42f,0.92f), new Color(0.20f,0.16f,0.38f), 0.73f),
            new PaletteKey(6.5f, new Color(0.94f,0.68f,0.58f), new Color(1f,0.48f,0.24f), new Color(1f,0.54f,0.28f), new Color(0.30f,0.18f,0.28f), 0.82f),
            new PaletteKey(8f, new Color(1f,0.88f,0.72f), new Color(1f,0.72f,0.42f), new Color(0.72f,0.74f,0.84f), new Color(0.30f,0.28f,0.32f), 0.94f),
            new PaletteKey(12f, Color.white, new Color(0.58f,0.82f,1f), new Color(0.42f,0.62f,0.86f), new Color(0.30f,0.34f,0.40f), 1f),
            new PaletteKey(15.5f, new Color(1f,0.94f,0.84f), new Color(0.72f,0.82f,0.94f), new Color(0.52f,0.58f,0.76f), new Color(0.32f,0.30f,0.34f), 0.97f),
            new PaletteKey(17.5f, new Color(1f,0.76f,0.52f), new Color(1f,0.54f,0.18f), new Color(1f,0.46f,0.16f), new Color(0.34f,0.20f,0.24f), 0.88f),
            new PaletteKey(19f, new Color(0.76f,0.54f,0.72f), new Color(0.90f,0.30f,0.22f), new Color(0.94f,0.28f,0.24f), new Color(0.20f,0.15f,0.34f), 0.76f),
            new PaletteKey(20.5f, new Color(0.48f,0.55f,0.82f), new Color(0.24f,0.38f,0.78f), new Color(0.26f,0.42f,0.94f), new Color(0.10f,0.14f,0.34f), 0.68f),
            new PaletteKey(22f, new Color(0.38f,0.48f,0.78f), new Color(0.14f,0.28f,0.70f), new Color(0.18f,0.38f,0.92f), new Color(0.08f,0.13f,0.32f), 0.64f)
        };
    }
}
