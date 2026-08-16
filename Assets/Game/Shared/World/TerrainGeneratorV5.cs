using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainGeneratorV5 : ProceduralGenerator<ZoneDNA, TerrainGenerationResult>
    {
        public static readonly GeneratorVersion GeneratorVersion = new GeneratorVersion(5);
        private readonly TerrainGenerationConfig _config; private readonly BiomeDefinition _biome;
        private readonly ZoneFeaturePlan _features; private readonly LargeScaleTerrainProfile _largeScale;
        private readonly TerrainMeshDataBuilder _meshBuilder = new TerrainMeshDataBuilder();
        public TerrainGeneratorV5(TerrainGenerationConfig config, BiomeDefinition biome, ZoneFeaturePlan features, LargeScaleTerrainProfile largeScale) : base(GeneratorVersion)
        { _config = config ?? throw new ArgumentNullException(nameof(config)); _biome = biome ?? throw new ArgumentNullException(nameof(biome)); _features = features ?? throw new ArgumentNullException(nameof(features)); _largeScale = largeScale ?? throw new ArgumentNullException(nameof(largeScale)); }
        protected override GenerationValidation ValidateCore(ZoneDNA dna, GenerationContext context)
        {
            if (dna.GeneratorVersion != Version) return GenerationValidation.Invalid("DNA_GENERATOR_VERSION_MISMATCH", "Zone DNA references another generator version.");
            if (dna.Seed != context.Seed) return GenerationValidation.Invalid("SEED_MISMATCH", "Zone DNA and context seeds differ.");
            if (dna.AssetCatalogVersion != context.AssetCatalogVersion) return GenerationValidation.Invalid("CATALOG_VERSION_MISMATCH", "Asset catalog versions differ.");
            if (dna.BiomeId != _biome.Id) return GenerationValidation.Invalid("BIOME_MISMATCH", "Biome does not match DNA.");
            return GenerationValidation.Valid();
        }
        protected override TerrainGenerationResult GenerateCore(ZoneDNA dna, GenerationContext context)
        {
            HeightFieldGeneratorV2 source = new HeightFieldGeneratorV2(dna, _config, _biome, _features, _largeScale);
            int resolution = _config.ResolvedResolution; float[] heights = new float[resolution * resolution]; float[] paths = new float[heights.Length];
            float spacingX = _config.Width / (resolution - 1); float spacingZ = _config.Depth / (resolution - 1);
            for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
            { HeightSample sample = source.Sample(-_config.Width * 0.5f + x * spacingX, -_config.Depth * 0.5f + z * spacingZ); int index = z * resolution + x; heights[index] = sample.Height; paths[index] = sample.PathMask; }
            TerrainHeightField field = new TerrainHeightField(resolution, _config.Width, _config.Depth, heights, paths);
            List<TerrainChunkData> chunks = new List<TerrainChunkData>(_config.ChunkCountX * _config.ChunkCountZ);
            for (int z = 0; z < _config.ChunkCountZ; z++) for (int x = 0; x < _config.ChunkCountX; x++) chunks.Add(_meshBuilder.BuildChunk(field, _config, _biome, x, z));
            return new TerrainGenerationResult(field, _config, chunks, GenerationFingerprint.ForTerrain(field, _config));
        }
    }
}
