using System;

namespace MyGameWorld.Shared.World
{
    public static class GenerationFingerprint
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong ForTerrain(TerrainHeightField field, TerrainGenerationConfig config)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            ulong hash = OffsetBasis;
            hash = Add(hash, config.ResolvedResolution);
            hash = Add(hash, config.TriangleCount);
            hash = Add(hash, config.ChunkCountX);
            hash = Add(hash, config.ChunkCountZ);
            float[] heights = field.CopyHeights();
            float[] paths = field.CopyPathMasks();
            for (int index = 0; index < heights.Length; index++)
            {
                hash = Add(hash, BitConverter.SingleToInt32Bits(heights[index]));
                hash = Add(hash, BitConverter.SingleToInt32Bits(paths[index]));
            }

            return hash;
        }

        public static ulong AddDecoration(ulong hash, DecorationPlacement placement)
        {
            hash = Add(hash, placement.StableId);
            hash = Add(hash, placement.Seed);
            hash = Add(hash, (int)placement.Kind);
            hash = Add(hash, placement.VisualAssetId.Value);
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.Position.X));
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.Position.Y));
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.Position.Z));
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.YawDegrees));
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.Scale));
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.ShapeA));
            hash = Add(hash, BitConverter.SingleToInt32Bits(placement.ShapeB));
            return Add(hash, BitConverter.SingleToInt32Bits(placement.ShapeC));
        }

        public static ulong AddElement(ulong hash, WorldElementDNA element)
        {
            hash = Add(hash, element.ElementId.Value);
            hash = Add(hash, (int)element.ElementKind);
            hash = Add(hash, element.Seed);
            hash = Add(hash, element.GeneratorVersion.Value);
            hash = Add(hash, BitConverter.SingleToInt32Bits(element.Bounds.CenterX));
            hash = Add(hash, BitConverter.SingleToInt32Bits(element.Bounds.CenterZ));
            return Add(hash, BitConverter.SingleToInt32Bits(element.Bounds.Radius));
        }

        private static ulong Add(ulong hash, long value)
        {
            ulong unsigned = unchecked((ulong)value);
            for (int shift = 0; shift < 64; shift += 8)
            {
                hash ^= (byte)(unsigned >> shift);
                hash *= Prime;
            }

            return hash;
        }
    }
}
