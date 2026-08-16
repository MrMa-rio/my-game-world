using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.World
{
    public static class WorldVisualAssetIds
    {
        public static readonly AssetId TemperateTree = new AssetId(10001);
        public static readonly AssetId TemperateRock = new AssetId(10002);
        public static readonly AssetId TemperateBush = new AssetId(10003);
        public static readonly AssetId TemperateFlower = new AssetId(10004);
        public static readonly AssetId TemperateFlowerCluster = new AssetId(10005);
        public static readonly AssetId TemperateMushroom = new AssetId(10006);
        public static readonly AssetId TemperateMushroomCluster = new AssetId(10007);
        public static readonly AssetId TemperateTreeCluster = new AssetId(10008);
        public static readonly AssetId TemperateRockCluster = new AssetId(10009);
        public static readonly AssetId TemperateBushCluster = new AssetId(10010);
        public static readonly AssetId WaterSurface = new AssetId(11001);
        public static readonly AssetId LavaSurface = new AssetId(11002);

        public static AssetId ForLiquid(LiquidSubstance substance)
        {
            switch (substance)
            {
                case LiquidSubstance.Water: return WaterSurface;
                case LiquidSubstance.Lava: return LavaSurface;
                default: throw new System.ArgumentOutOfRangeException(nameof(substance));
            }
        }
        public static readonly AssetId DevelopmentScaleMarker = new AssetId(10900);

        public static AssetId ForDecoration(DecorationKind kind)
        {
            switch (kind)
            {
                case DecorationKind.Tree: return TemperateTree;
                case DecorationKind.Rock: return TemperateRock;
                case DecorationKind.Bush: return TemperateBush;
                case DecorationKind.Flower: return TemperateFlower;
                case DecorationKind.FlowerCluster: return TemperateFlowerCluster;
                case DecorationKind.Mushroom: return TemperateMushroom;
                case DecorationKind.MushroomCluster: return TemperateMushroomCluster;
                case DecorationKind.TreeCluster: return TemperateTreeCluster;
                case DecorationKind.RockCluster: return TemperateRockCluster;
                case DecorationKind.BushCluster: return TemperateBushCluster;
                default: return DevelopmentScaleMarker;
            }
        }
    }
}
