namespace MyGameWorld.Shared.World
{
    public enum BiomeId : ushort
    {
        TemperateGrassland = 1
    }

    public enum TerrainProfileId : ushort
    {
        RollingLowPoly = 1
    }

    public enum TerrainShadingMode : byte
    {
        Flat = 1,
        Smooth = 2
    }

    public enum DecorationKind : byte
    {
        Tree = 1,
        Rock = 2,
        Bush = 3,
        ScaleMarker = 4,
        Flower = 5,
        FlowerCluster = 6,
        Mushroom = 7,
        MushroomCluster = 8,
        TreeCluster = 9,
        RockCluster = 10,
        BushCluster = 11
    }
}
