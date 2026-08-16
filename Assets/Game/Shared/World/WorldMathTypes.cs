using System;

namespace MyGameWorld.Shared.World
{
    public readonly struct WorldVector3 : IEquatable<WorldVector3>
    {
        public WorldVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public bool Equals(WorldVector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj) => obj is WorldVector3 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                return (hash * 397) ^ Z.GetHashCode();
            }
        }
    }

    public readonly struct WorldColor : IEquatable<WorldColor>
    {
        public WorldColor(float red, float green, float blue)
        {
            Red = Clamp01(red);
            Green = Clamp01(green);
            Blue = Clamp01(blue);
        }

        public float Red { get; }

        public float Green { get; }

        public float Blue { get; }

        public bool Equals(WorldColor other)
        {
            return Red.Equals(other.Red) && Green.Equals(other.Green) && Blue.Equals(other.Blue);
        }

        public override bool Equals(object obj) => obj is WorldColor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Red.GetHashCode();
                hash = (hash * 397) ^ Green.GetHashCode();
                return (hash * 397) ^ Blue.GetHashCode();
            }
        }

        public static WorldColor Lerp(WorldColor first, WorldColor second, float amount)
        {
            float t = Clamp01(amount);
            return new WorldColor(
                first.Red + ((second.Red - first.Red) * t),
                first.Green + ((second.Green - first.Green) * t),
                first.Blue + ((second.Blue - first.Blue) * t));
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
