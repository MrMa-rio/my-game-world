using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public static class WorldPhysicsLayers
    {
        public const int Actor = 8;
        public const int Terrain = 9;
        public const int StaticWorld = 10;
        public const int DynamicWorld = 11;
        public const int SoftEnvironment = 12;
        public const int Trigger = 13;
        public const int Interaction = 14;
        public const int Projectile = 15;
        public const int Water = 4;

        public static int GroundMask => (1 << Terrain) | (1 << StaticWorld) | (1 << DynamicWorld);
        public static bool IsSolid(int layer) => layer == Terrain || layer == StaticWorld || layer == DynamicWorld;
    }

    public enum PhysicalInteractionLevel : byte
    {
        None = 0,
        Soft = 1,
        Solid = 2
    }
}
