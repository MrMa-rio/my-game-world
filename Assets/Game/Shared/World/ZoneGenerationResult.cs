using System;
using System.Collections.Generic;

namespace MyGameWorld.Shared.World
{
    public sealed class ZoneGenerationResult
    {
        private readonly DecorationPlacement[] _decorations;

        public ZoneGenerationResult(
            ZoneDNA dna,
            TerrainGenerationResult terrain,
            IReadOnlyList<DecorationPlacement> decorations,
            ZoneFeaturePlan features)
        {
            DNA = dna ?? throw new ArgumentNullException(nameof(dna));
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            Features = features ?? throw new ArgumentNullException(nameof(features));
            if (decorations == null)
            {
                throw new ArgumentNullException(nameof(decorations));
            }

            _decorations = new DecorationPlacement[decorations.Count];
            ulong fingerprint = GenerationFingerprint.AddElement(terrain.Fingerprint, Features.Terrain);
            for (int index = 0; index < Features.Landforms.Count; index++) fingerprint = GenerationFingerprint.AddElement(fingerprint, Features.Landforms[index]);
            for (int index = 0; index < Features.Paths.Count; index++) fingerprint = GenerationFingerprint.AddElement(fingerprint, Features.Paths[index]);
            for (int index = 0; index < Features.Liquids.Count; index++) fingerprint = GenerationFingerprint.AddLiquid(fingerprint, Features.Liquids[index]);
            for (int index = 0; index < decorations.Count; index++)
            {
                _decorations[index] = decorations[index];
                fingerprint = GenerationFingerprint.AddDecoration(fingerprint, decorations[index]);
            }

            Fingerprint = fingerprint;
        }

        public ZoneDNA DNA { get; }

        public TerrainGenerationResult Terrain { get; }
        public ZoneFeaturePlan Features { get; }

        public IReadOnlyList<WorldElementDNA> ResolveTerrainContact(float worldX, float worldZ) => Features.ResolveAt(worldX, worldZ);

        public IReadOnlyList<DecorationPlacement> Decorations => _decorations;

        public ulong Fingerprint { get; }
    }
}
