using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class ProceduralLodResolver
    {
        public ProceduralVisualLod Resolve(DecorationPlacement definition, Vector3 viewerPosition)
        {
            Vector3 position = new Vector3(definition.Position.X, definition.Position.Y, definition.Position.Z);
            float distance = Vector3.Distance(position, viewerPosition);
            float sizeBias = Mathf.Max(0.65f, definition.Scale);
            if (definition.Kind == DecorationKind.Tree)
            {
                if (distance < 520f * sizeBias) return ProceduralVisualLod.High;
                return distance < 900f * sizeBias ? ProceduralVisualLod.Medium : ProceduralVisualLod.Low;
            }
            if (distance < 24f * sizeBias) return ProceduralVisualLod.High;
            return distance < 52f * sizeBias ? ProceduralVisualLod.Medium : ProceduralVisualLod.Low;
        }

        public int ResolveSegments(ProceduralVisualLod lod)
        {
            switch (lod)
            {
                case ProceduralVisualLod.High: return 8;
                case ProceduralVisualLod.Medium: return 6;
                default: return 4;
            }
        }

        public int EstimateVertexCount(DecorationKind kind, ProceduralVisualLod lod)
        {
            int segments = Mathf.CeilToInt(ResolveSegments(lod) * 1.15f);
            switch (kind)
            {
                case DecorationKind.Tree:
                    return segments * (lod == ProceduralVisualLod.High ? 210 : lod == ProceduralVisualLod.Medium ? 165 : 75);
                case DecorationKind.Bush: return segments * (lod == ProceduralVisualLod.Low ? 6 : 18);
                case DecorationKind.Rock: return segments * 12;
                default: return segments * 18;
            }
        }
    }
}
