using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;

namespace MyGameWorld.Shared.World
{
    public sealed class TerrainGeneratorV1 : ProceduralGenerator<ZoneDNA, TerrainGenerationResult>
    {
        public static readonly GeneratorVersion GeneratorVersion = new GeneratorVersion(1);

        private readonly TerrainGenerationConfig _config;
        private readonly BiomeDefinition _biome;
        private readonly ZoneFeaturePlan _features;
        private readonly TerrainMeshDataBuilder _meshBuilder = new TerrainMeshDataBuilder();

        public TerrainGeneratorV1(TerrainGenerationConfig config, BiomeDefinition biome, ZoneFeaturePlan features)
            : base(GeneratorVersion)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _biome = biome ?? throw new ArgumentNullException(nameof(biome));
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        public TerrainChunkData GenerateChunk(
            ZoneDNA dna,
            GenerationContext context,
            int chunkX,
            int chunkZ)
        {
            TerrainGenerationResult result = Generate(dna, context);
            for (int index = 0; index < result.Chunks.Count; index++)
            {
                TerrainChunkData chunk = result.Chunks[index];
                if (chunk.ChunkX == chunkX && chunk.ChunkZ == chunkZ)
                {
                    return chunk;
                }
            }

            throw new ArgumentOutOfRangeException("Chunk coordinate is outside the zone.");
        }

        protected override GenerationValidation ValidateCore(ZoneDNA dna, GenerationContext context)
        {
            if (dna.GeneratorVersion != Version)
            {
                return GenerationValidation.Invalid("DNA_GENERATOR_VERSION_MISMATCH", "Zone DNA references another generator version.");
            }

            if (dna.Seed != context.Seed)
            {
                return GenerationValidation.Invalid("SEED_MISMATCH", "Zone DNA and generation context must use the same seed.");
            }

            if (dna.AssetCatalogVersion != context.AssetCatalogVersion)
            {
                return GenerationValidation.Invalid("CATALOG_VERSION_MISMATCH", "Zone DNA and context catalog versions differ.");
            }

            if (dna.BiomeId != _biome.Id)
            {
                return GenerationValidation.Invalid("BIOME_MISMATCH", "The supplied biome does not match the zone DNA.");
            }

            if (dna.TerrainProfileId != TerrainProfileId.RollingLowPoly)
            {
                return GenerationValidation.Invalid("TERRAIN_PROFILE_UNSUPPORTED", "TerrainGeneratorV1 supports RollingLowPoly only.");
            }

            return GenerationValidation.Valid();
        }

        protected override TerrainGenerationResult GenerateCore(ZoneDNA dna, GenerationContext context)
        {
            HeightFieldGeneratorV1 heightGenerator = new HeightFieldGeneratorV1(dna, _config, _biome, _features);
            int resolution = _config.ResolvedResolution;
            float[] heights = new float[resolution * resolution];
            float[] paths = new float[heights.Length];
            float spacingX = _config.Width / (resolution - 1);
            float spacingZ = _config.Depth / (resolution - 1);

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float worldX = (-_config.Width * 0.5f) + (x * spacingX);
                    float worldZ = (-_config.Depth * 0.5f) + (z * spacingZ);
                    HeightSample sample = heightGenerator.Sample(worldX, worldZ);
                    int index = (z * resolution) + x;
                    heights[index] = sample.Height;
                    paths[index] = sample.PathMask;
                }
            }

            TerrainHeightField field = new TerrainHeightField(
                resolution,
                _config.Width,
                _config.Depth,
                heights,
                paths);
            List<TerrainChunkData> chunks = new List<TerrainChunkData>(_config.ChunkCountX * _config.ChunkCountZ);
            for (int chunkZ = 0; chunkZ < _config.ChunkCountZ; chunkZ++)
            {
                for (int chunkX = 0; chunkX < _config.ChunkCountX; chunkX++)
                {
                    chunks.Add(_meshBuilder.BuildChunk(field, _config, _biome, chunkX, chunkZ));
                }
            }

            ulong fingerprint = GenerationFingerprint.ForTerrain(field, _config);
            return new TerrainGenerationResult(field, _config, chunks, fingerprint);
        }
    }
}
