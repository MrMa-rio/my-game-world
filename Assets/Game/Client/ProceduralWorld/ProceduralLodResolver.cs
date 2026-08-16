using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class ProceduralLodResolver
    {
        private float _lodBias = 1f;
        private float _subpixelThreshold = 1.5f;
        public void ConfigureImageStability(float lodBias, float subpixelThreshold)
        { _lodBias = Mathf.Clamp(lodBias, 0.5f, 3f); _subpixelThreshold = Mathf.Clamp(subpixelThreshold, 0.5f, 4f); }

        public ProceduralVisualLod Resolve(DecorationPlacement definition, Vector3 viewerPosition)
        {
            Vector3 position = new Vector3(definition.Position.X, definition.Position.Y, definition.Position.Z);
            float distance = Vector3.Distance(position, viewerPosition);
            float sizeBias = Mathf.Max(0.65f, definition.Scale);
            float projectedSize = definition.Scale * 900f / Mathf.Max(1f, distance);
            if (projectedSize < _subpixelThreshold) return ProceduralVisualLod.Low;
            if (definition.Kind == DecorationKind.Tree || definition.Kind == DecorationKind.TreeCluster)
            {
                if (distance < 520f * sizeBias * _lodBias) return ProceduralVisualLod.High;
                return distance < 900f * sizeBias * _lodBias ? ProceduralVisualLod.Medium : ProceduralVisualLod.Low;
            }
            if (distance < 24f * sizeBias * _lodBias) return ProceduralVisualLod.High;
            return distance < 52f * sizeBias * _lodBias ? ProceduralVisualLod.Medium : ProceduralVisualLod.Low;
        }

        public ProceduralVisualLod ResolveStable(DecorationPlacement definition, Vector3 viewerPosition, ProceduralVisualLod previous)
        {
            ProceduralVisualLod desired = Resolve(definition, viewerPosition);
            if (desired == previous) return desired;
            float distance = Vector3.Distance(new Vector3(definition.Position.X, definition.Position.Y, definition.Position.Z), viewerPosition);
            float sizeBias = Mathf.Max(0.65f, definition.Scale) * _lodBias;
            bool tree = definition.Kind == DecorationKind.Tree || definition.Kind == DecorationKind.TreeCluster;
            float boundary = previous == ProceduralVisualLod.High || desired == ProceduralVisualLod.High
                ? (tree ? 520f : 24f) * sizeBias : (tree ? 900f : 52f) * sizeBias;
            return Mathf.Abs(distance - boundary) <= boundary * 0.08f ? previous : desired;
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
