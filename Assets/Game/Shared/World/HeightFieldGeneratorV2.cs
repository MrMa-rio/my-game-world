using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public sealed class HeightFieldGeneratorV2
    {
        private const uint RegionalScope = 0x5245474E;
        private const uint MacroScope = 0x4D414352;
        private const uint DetailScope = 0x4445544C;
        private readonly TerrainGenerationConfig _config;
        private readonly BiomeDefinition _biome;
        private readonly ZoneFeaturePlan _features;
        private readonly long _regionalSeed;
        private readonly long _macroSeed;
        private readonly long _detailSeed;

        public HeightFieldGeneratorV2(ZoneDNA dna, TerrainGenerationConfig config, BiomeDefinition biome, ZoneFeaturePlan features)
        {
            if (dna == null) throw new ArgumentNullException(nameof(dna));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _biome = biome ?? throw new ArgumentNullException(nameof(biome));
            _features = features ?? throw new ArgumentNullException(nameof(features));
            _regionalSeed = SeedDerivation.Derive(dna.Seed, RegionalScope, dna.ZoneId.Value);
            _macroSeed = SeedDerivation.Derive(dna.Seed, MacroScope, dna.ZoneId.Value);
            _detailSeed = SeedDerivation.Derive(dna.Seed, DetailScope, dna.ZoneId.Value);
        }

        public HeightSample Sample(float worldX, float worldZ)
        {
            double regional = DeterministicNoise2D.FractalBrownianMotion(_regionalSeed,
                worldX / (_biome.MacroScale * 5.5d), worldZ / (_biome.MacroScale * 5.5d), 3);
            double macro = DeterministicNoise2D.FractalBrownianMotion(_macroSeed,
                worldX / _biome.MacroScale, worldZ / _biome.MacroScale, _biome.MacroOctaves);
            double detail = DeterministicNoise2D.FractalBrownianMotion(_detailSeed,
                worldX / _biome.DetailScale, worldZ / _biome.DetailScale, _biome.DetailOctaves);
            double height = _biome.BaseHeight
                + (regional * _biome.MacroAmplitude * 1.05d)
                + (macro * _biome.MacroAmplitude * 0.58d);

            for (int i = 0; i < _features.Landforms.Count; i++)
            {
                LandformDNA feature = _features.Landforms[i];
                float influence = TerrainFeatureMath.RadialInfluence(feature.Bounds, worldX, worldZ, feature.FalloffPower);
                if (feature.ElementKind == WorldElementKind.Plateau)
                    height = Lerp(height, _biome.BaseHeight + feature.Amplitude + regional * 2d, influence * 0.7d);
                else height += feature.Amplitude * influence;
            }

            double pathMask = 0d;
            for (int i = 0; i < _features.Paths.Count; i++)
            {
                PathDNA path = _features.Paths[i];
                float influence = TerrainFeatureMath.PathInfluence(path, worldX, worldZ);
                pathMask = Math.Max(pathMask, influence);
                double pathHeight = _biome.BaseHeight + regional * _biome.MacroAmplitude * 0.55d + macro * _biome.MacroAmplitude * 0.16d;
                height = Lerp(height, pathHeight, influence * path.FlattenStrength);
            }

            height += detail * _biome.DetailAmplitude * (1d - pathMask * 0.82d);
            return new HeightSample((float)Math.Max(0d, Math.Min(_config.MaxHeight, height)), (float)pathMask);
        }

        private static double Lerp(double first, double second, double amount) => first + ((second - first) * amount);
    }
}
