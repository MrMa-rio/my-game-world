using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public static class WorldVisualAssetIds
    {
        public static readonly AssetId TemperateTree = new AssetId(10001);
        public static readonly AssetId TemperateRock = new AssetId(10002);
        public static readonly AssetId TemperateBush = new AssetId(10003);
        public static readonly AssetId DevelopmentScaleMarker = new AssetId(10900);

        public static AssetId ForDecoration(DecorationKind kind)
        {
            switch (kind)
            {
                case DecorationKind.Tree: return TemperateTree;
                case DecorationKind.Rock: return TemperateRock;
                case DecorationKind.Bush: return TemperateBush;
                default: return DevelopmentScaleMarker;
            }
        }
    }
}
