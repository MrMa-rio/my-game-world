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
        private readonly long _largeScaleSeed;
        private readonly LargeScaleTerrainProfile _largeScaleProfile;

        public HeightFieldGeneratorV2(ZoneDNA dna, TerrainGenerationConfig config, BiomeDefinition biome,
            ZoneFeaturePlan features, LargeScaleTerrainProfile largeScaleProfile = null)
        {
            if (dna == null) throw new ArgumentNullException(nameof(dna));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _biome = biome ?? throw new ArgumentNullException(nameof(biome));
            _features = features ?? throw new ArgumentNullException(nameof(features));
            _regionalSeed = SeedDerivation.Derive(dna.Seed, RegionalScope, dna.ZoneId.Value);
            _macroSeed = SeedDerivation.Derive(dna.Seed, MacroScope, dna.ZoneId.Value);
            _detailSeed = SeedDerivation.Derive(dna.Seed, DetailScope, dna.ZoneId.Value);
            _largeScaleProfile = largeScaleProfile;
            _largeScaleSeed = SeedDerivation.Derive(dna.Seed, 0x4C53434C, largeScaleProfile != null ? largeScaleProfile.Version : 0);
        }

        public HeightSample Sample(float worldX, float worldZ)
        {
            return Sample(worldX, worldZ, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro);
        }

        public HeightSample Sample(double worldX, double worldZ, TerrainFrequencyBand bands)
        {
            double regional = DeterministicNoise2D.FractalBrownianMotion(_regionalSeed,
                worldX / (_biome.MacroScale * 5.5d), worldZ / (_biome.MacroScale * 5.5d), 3);
            double macro = DeterministicNoise2D.FractalBrownianMotion(_macroSeed,
                worldX / _biome.MacroScale, worldZ / _biome.MacroScale, _biome.MacroOctaves);
            double detail = 0d;
            if ((bands & TerrainFrequencyBand.Micro) != 0)
                detail = DeterministicNoise2D.FractalBrownianMotion(_detailSeed,
                    worldX / _biome.DetailScale, worldZ / _biome.DetailScale, _biome.DetailOctaves);
            double height = _biome.BaseHeight
                + (regional * _biome.MacroAmplitude * 1.05d)
                + (macro * _biome.MacroAmplitude * 0.58d);

            if (_largeScaleProfile != null && (bands & TerrainFrequencyBand.Macro) != 0)
            {
                double ridgeNoise = DeterministicNoise2D.FractalBrownianMotion(_largeScaleSeed,
                    worldX / _largeScaleProfile.MountainScale, worldZ / _largeScaleProfile.MountainScale, 4);
                double ridge = Math.Pow(Math.Max(0d, 1d - Math.Abs(ridgeNoise)), _largeScaleProfile.RidgeSharpness);
                double mountainMask = Math.Max(0d, DeterministicNoise2D.FractalBrownianMotion(_largeScaleSeed ^ 0x4D41534B,
                    worldX / (_largeScaleProfile.MountainScale * 1.8d), worldZ / (_largeScaleProfile.MountainScale * 1.8d), 3) + 0.18d);
                double valley = DeterministicNoise2D.FractalBrownianMotion(_largeScaleSeed ^ 0x56414C4C,
                    worldX / _largeScaleProfile.ValleyScale, worldZ / _largeScaleProfile.ValleyScale, 3);
                height += ridge * mountainMask * _largeScaleProfile.MountainAmplitude;
                height += (ridgeNoise + 1d) * _largeScaleProfile.MountainAmplitude * 0.45d;
                height += valley * _largeScaleProfile.ValleyAmplitude;
            }

            if ((bands & TerrainFrequencyBand.Macro) != 0)
            for (int i = 0; i < _features.Landforms.Count; i++)
            {
                LandformDNA feature = _features.Landforms[i];
                float influence = TerrainFeatureMath.RadialInfluence(feature.Bounds, (float)worldX, (float)worldZ, feature.FalloffPower);
                if (_largeScaleProfile != null && _largeScaleProfile.Version >= 2)
                    height += GeologicalLandformModel.Evaluate(feature, worldX, worldZ);
                else if (feature.ElementKind == WorldElementKind.Plateau)
                    height = Lerp(height, _biome.BaseHeight + feature.Amplitude + regional * 2d, influence * 0.7d);
                else height += feature.Amplitude * influence;
            }

            double pathMask = 0d;
            if ((bands & TerrainFrequencyBand.Meso) != 0)
            for (int i = 0; i < _features.Paths.Count; i++)
            {
                PathDNA path = _features.Paths[i];
                float influence = TerrainFeatureMath.PathInfluence(path, (float)worldX, (float)worldZ);
                pathMask = Math.Max(pathMask, influence);
                double pathHeight = _biome.BaseHeight + regional * _biome.MacroAmplitude * 0.55d + macro * _biome.MacroAmplitude * 0.16d;
                height = Lerp(height, pathHeight, influence * path.FlattenStrength);
            }

            if ((bands & TerrainFrequencyBand.Micro) != 0)
                height += detail * _biome.DetailAmplitude * (1d - pathMask * 0.82d);
            return new HeightSample((float)Math.Max(0d, Math.Min(_config.MaxHeight, height)), (float)pathMask);
        }

        private static double Lerp(double first, double second, double amount) => first + ((second - first) * amount);
    }
}
