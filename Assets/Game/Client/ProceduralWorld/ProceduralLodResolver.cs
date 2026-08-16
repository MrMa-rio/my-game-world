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
            if (definition.Kind == DecorationKind.Tree || definition.Kind == DecorationKind.TreeCluster)
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
                case DecorationKind.TreeCluster: return segments * (lod == ProceduralVisualLod.High ? 1050 : lod == ProceduralVisualLod.Medium ? 660 : 225);
                case DecorationKind.Bush: return segments * (lod == ProceduralVisualLod.Low ? 6 : 18);
                case DecorationKind.Rock: return segments * (lod == ProceduralVisualLod.High ? 90 : lod == ProceduralVisualLod.Medium ? 36 : 24);
                case DecorationKind.Flower: return lod == ProceduralVisualLod.High ? 480 : lod == ProceduralVisualLod.Medium ? 360 : 270;
                case DecorationKind.FlowerCluster: return lod == ProceduralVisualLod.High ? 3400 : lod == ProceduralVisualLod.Medium ? 1900 : 850;
                case DecorationKind.Mushroom: return lod == ProceduralVisualLod.High ? 320 : 110;
                case DecorationKind.MushroomCluster: return lod == ProceduralVisualLod.High ? 1900 : lod == ProceduralVisualLod.Medium ? 520 : 340;
                case DecorationKind.RockCluster: return lod == ProceduralVisualLod.High ? 2400 : lod == ProceduralVisualLod.Medium ? 720 : 360;
                case DecorationKind.BushCluster: return lod == ProceduralVisualLod.High ? 2100 : lod == ProceduralVisualLod.Medium ? 680 : 240;
                default: return segments * 18;
            }
        }
    }
}
