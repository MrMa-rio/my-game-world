using System;

namespace MyGameWorld.Shared.Procedural
{
    [Flags]
    public enum AssetTrait : ulong
    {
        None = 0,
        HumanoidSkeleton = 1UL << 0,
        CreatureSkeleton = 1UL << 1,
        SmallFrame = 1UL << 2,
        MediumFrame = 1UL << 3,
        LargeFrame = 1UL << 4,
        HeadSocket = 1UL << 5,
        HairSocket = 1UL << 6,
        HandSocket = 1UL << 7,
        TwoHanded = 1UL << 8,
        Exterior = 1UL << 9,
        Interior = 1UL << 10,
        MasculineFrame = 1UL << 11,
        FeminineFrame = 1UL << 12
    }
}
