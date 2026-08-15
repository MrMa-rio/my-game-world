using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public readonly struct HeightSample
    {
        public HeightSample(float height, float pathMask)
        {
            Height = height;
            PathMask = pathMask;
        }

        public float Height { get; }

        public float PathMask { get; }
    }

    public sealed class HeightFieldGeneratorV1
    {
        private const uint MacroScope = 0x4D414352;
        private const uint DetailScope = 0x4445544C;

        private readonly TerrainGenerationConfig _config;
        private readonly BiomeDefinition _biome;
        private readonly long _macroSeed;
        private readonly long _detailSeed;
        private readonly ZoneFeaturePlan _features;

        public HeightFieldGeneratorV1(ZoneDNA dna, TerrainGenerationConfig config, BiomeDefinition biome, ZoneFeaturePlan features)
        {
            if (dna == null)
            {
                throw new ArgumentNullException(nameof(dna));
            }

            _config = config ?? throw new ArgumentNullException(nameof(config));
            _biome = biome ?? throw new ArgumentNullException(nameof(biome));
            _features = features ?? throw new ArgumentNullException(nameof(features));
            _macroSeed = SeedDerivation.Derive(dna.Seed, MacroScope, dna.ZoneId.Value);
            _detailSeed = SeedDerivation.Derive(dna.Seed, DetailScope, dna.ZoneId.Value);
        }

        public HeightSample Sample(float worldX, float worldZ)
        {
            double macro = DeterministicNoise2D.FractalBrownianMotion(
                _macroSeed,
                worldX / _biome.MacroScale,
                worldZ / _biome.MacroScale,
                _biome.MacroOctaves);
            double detail = DeterministicNoise2D.FractalBrownianMotion(
                _detailSeed,
                worldX / _biome.DetailScale,
                worldZ / _biome.DetailScale,
                _biome.DetailOctaves);
            double height = _biome.BaseHeight + (macro * _biome.MacroAmplitude * 0.48d);
            for (int i = 0; i < _features.Landforms.Count; i++)
            {
                LandformDNA feature = _features.Landforms[i];
                float influence = TerrainFeatureMath.RadialInfluence(feature.Bounds, worldX, worldZ, feature.FalloffPower);
                if (feature.ElementKind == WorldElementKind.Plateau)
                {
                    double target = _biome.BaseHeight + feature.Amplitude;
                    height = Lerp(height, target, influence * 0.72d);
                }
                else height += feature.Amplitude * influence;
            }

            double pathMask = 0d;
            for (int i = 0; i < _features.Paths.Count; i++)
            {
                PathDNA path = _features.Paths[i];
                float influence = TerrainFeatureMath.PathInfluence(path, worldX, worldZ);
                if (influence > pathMask) pathMask = influence;
                height = Lerp(height, _biome.BaseHeight + macro * _biome.MacroAmplitude * 0.2d, influence * path.FlattenStrength);
            }
            height += detail * _biome.DetailAmplitude * (1d - (pathMask * 0.85d));
            height = Math.Max(0d, Math.Min(_config.MaxHeight, height));

            return new HeightSample((float)height, (float)pathMask);
        }

        private static double SmoothStep(double minimum, double maximum, double value)
        {
            double t = Math.Max(0d, Math.Min(1d, (value - minimum) / (maximum - minimum)));
            return t * t * (3d - (2d * t));
        }

        private static double Lerp(double first, double second, double amount)
        {
            return first + ((second - first) * amount);
        }
    }
}
