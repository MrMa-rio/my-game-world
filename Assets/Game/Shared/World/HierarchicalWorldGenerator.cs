using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public readonly struct HierarchicalHeightSample
    {
        public HierarchicalHeightSample(float height, float macro, float meso, float micro, WorldColor color, float forestCoverage)
        { Height = height; Macro = macro; Meso = meso; Micro = micro; Color = color; ForestCoverage = forestCoverage; }
        public float Height { get; }
        public float Macro { get; }
        public float Meso { get; }
        public float Micro { get; }
        public WorldColor Color { get; }
        public float ForestCoverage { get; }
    }

    public sealed class HierarchicalWorldGenerator
    {
        private const long MacroSalt = 0x4D4143524F;
        private const long MesoSalt = 0x4D45534F;
        private const long MicroSalt = 0x4D4943524F;
        private const long ForestSalt = 0x464F52455354;
        private readonly long _seed;
        private readonly HeightFieldGeneratorV2 _sharedHeightSource;
        private readonly BiomeDefinition _sharedBiome;
        private readonly TerrainGenerationConfig _sharedConfig;
        public HierarchicalWorldGenerator(long seed, ushort generationVersion)
        { _seed = unchecked(seed ^ ((long)generationVersion << 48)); GenerationVersion = generationVersion; }
        public HierarchicalWorldGenerator(HeightFieldGeneratorV2 sharedHeightSource, BiomeDefinition biome,
            TerrainGenerationConfig config, long worldSeed, ushort generationVersion)
        {
            _sharedHeightSource = sharedHeightSource ?? throw new ArgumentNullException(nameof(sharedHeightSource));
            _sharedBiome = biome ?? throw new ArgumentNullException(nameof(biome));
            _sharedConfig = config ?? throw new ArgumentNullException(nameof(config));
            _seed = unchecked(worldSeed ^ ((long)generationVersion << 48));
            GenerationVersion = generationVersion;
        }
        public ushort GenerationVersion { get; }

        public HierarchicalHeightSample Sample(double globalX, double globalZ, TerrainFrequencyBand bands)
        {
            if (_sharedHeightSource != null)
            {
                HeightSample full = _sharedHeightSource.Sample(globalX, globalZ, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso | TerrainFrequencyBand.Micro);
                HeightSample sharedMacro = _sharedHeightSource.Sample(globalX, globalZ, TerrainFrequencyBand.Macro);
                HeightSample macroMeso = _sharedHeightSource.Sample(globalX, globalZ, TerrainFrequencyBand.Macro | TerrainFrequencyBand.Meso);
                HeightSample selected = _sharedHeightSource.Sample(globalX, globalZ, bands);
                float normalized = Math.Max(0f, Math.Min(1f, selected.Height / _sharedConfig.MaxHeight));
                WorldColor sharedColor = _sharedBiome.ResolveTerrainColor(normalized, 1f, selected.PathMask);
                float sharedForest = Clamp01((float)((DeterministicNoise2D.FractalBrownianMotion(
                    _seed ^ 0x464F52455354L, globalX / 1400d, globalZ / 1400d, 3) + 1d) * 0.5d));
                return new HierarchicalHeightSample(selected.Height, sharedMacro.Height,
                    macroMeso.Height - sharedMacro.Height, full.Height - macroMeso.Height, sharedColor, sharedForest);
            }
            // Every representation samples the same bands at the same global coordinates. LOD only omits high frequency terms.
            double continental = DeterministicNoise2D.FractalBrownianMotion(_seed ^ MacroSalt, globalX / 18000d, globalZ / 18000d, 3);
            double ridgeNoise = DeterministicNoise2D.FractalBrownianMotion(_seed ^ (MacroSalt * 3), globalX / 6200d, globalZ / 6200d, 4);
            double ridge = Math.Pow(Math.Max(0d, 1d - Math.Abs(ridgeNoise)), 3d);
            double macro = 80d + continental * 260d + ridge * Math.Max(0d, continental + 0.3d) * 1250d;
            double meso = DeterministicNoise2D.FractalBrownianMotion(_seed ^ MesoSalt, globalX / 900d, globalZ / 900d, 4) * 95d;
            double micro = DeterministicNoise2D.FractalBrownianMotion(_seed ^ MicroSalt, globalX / 90d, globalZ / 90d, 3) * 9d;
            double height = (bands & TerrainFrequencyBand.Macro) != 0 ? macro : 0d;
            if ((bands & TerrainFrequencyBand.Meso) != 0) height += meso;
            if ((bands & TerrainFrequencyBand.Micro) != 0) height += micro;
            float forest = Clamp01((float)(DeterministicNoise2D.FractalBrownianMotion(_seed ^ ForestSalt, globalX / 1400d, globalZ / 1400d, 3) * 0.7d + 0.48d));
            float altitude = Clamp01((float)(height / 1500d));
            WorldColor low = new WorldColor(0.18f, 0.42f, 0.20f);
            WorldColor rock = new WorldColor(0.38f, 0.39f, 0.36f);
            WorldColor snow = new WorldColor(0.82f, 0.86f, 0.82f);
            WorldColor color = WorldColor.Lerp(low, rock, Clamp01(altitude * 1.35f));
            color = WorldColor.Lerp(color, snow, Clamp01((altitude - 0.72f) * 3.5f));
            color = WorldColor.Lerp(color, new WorldColor(0.10f, 0.29f, 0.14f), forest * (1f - altitude) * 0.55f);
            return new HierarchicalHeightSample((float)Math.Max(-40d, height), (float)macro, (float)meso, (float)micro, color, forest);
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }

    public sealed class TerrainRepresentationData
    {
        public TerrainRepresentationData(WorldCellCoordinate coordinate, WorldBounds bounds, WorldRepresentationKind level,
            WorldVector3[] vertices, WorldColor[] colors, int[] triangles, float geometricError, float forestCoverage)
        { Coordinate = coordinate; Bounds = bounds; Level = level; Vertices = vertices; Colors = colors; Triangles = triangles; GeometricError = geometricError; ForestCoverage = forestCoverage; }
        public WorldCellCoordinate Coordinate { get; }
        public WorldBounds Bounds { get; }
        public WorldRepresentationKind Level { get; }
        public WorldVector3[] Vertices { get; }
        public WorldColor[] Colors { get; }
        public int[] Triangles { get; }
        public float GeometricError { get; }
        public float ForestCoverage { get; }
    }

    public sealed class TerrainRepresentationBuilder
    {
        public TerrainRepresentationData Build(WorldCell cell, WorldRepresentationLevel level, HierarchicalWorldGenerator generator)
        {
            return Build(cell, level, generator, null);
        }

        public TerrainRepresentationData Build(WorldCell cell, WorldRepresentationLevel level,
            HierarchicalWorldGenerator generator, WorldBounds? exclusionBounds)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            int resolution = level.TerrainResolution;
            WorldVector3[] vertices = new WorldVector3[resolution * resolution];
            WorldColor[] colors = new WorldColor[vertices.Length];
            double step = cell.Bounds.Size / (resolution - 1);
            float forest = 0f;
            for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
            {
                double gx = cell.Bounds.MinimumX + x * step;
                double gz = cell.Bounds.MinimumZ + z * step;
                HierarchicalHeightSample sample = generator.Sample(gx, gz, level.Frequencies);
                int index = z * resolution + x;
                vertices[index] = new WorldVector3((float)(x * step), sample.Height, (float)(z * step));
                colors[index] = sample.Color; forest += sample.ForestCoverage;
            }
            List<int> triangles = new List<int>((resolution - 1) * (resolution - 1) * 6);
            for (int z = 0; z < resolution - 1; z++) for (int x = 0; x < resolution - 1; x++)
            {
                double centerX = cell.Bounds.MinimumX + (x + 0.5d) * step;
                double centerZ = cell.Bounds.MinimumZ + (z + 0.5d) * step;
                if (exclusionBounds.HasValue && exclusionBounds.Value.Contains(centerX, centerZ)) continue;
                int a = z * resolution + x, b = a + resolution, c = a + 1, d = b + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(c); triangles.Add(b); triangles.Add(d);
            }
            return new TerrainRepresentationData(cell.Coordinate, cell.Bounds, level.Kind, vertices, colors, triangles.ToArray(),
                (float)(step * 0.5d), forest / vertices.Length);
        }
    }
}
