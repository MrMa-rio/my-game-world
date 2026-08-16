using System;

namespace MyGameWorld.Shared.World
{
    public static class DeterministicNoise2D
    {
        public static double FractalBrownianMotion(
            long seed,
            double x,
            double y,
            int octaves,
            double lacunarity = 2d,
            double persistence = 0.5d)
        {
            double total = 0d;
            double amplitude = 1d;
            double frequency = 1d;
            double amplitudeSum = 0d;

            for (int octave = 0; octave < octaves; octave++)
            {
                total += Sample(seed + (octave * 7919L), x * frequency, y * frequency) * amplitude;
                amplitudeSum += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / amplitudeSum;
        }

        public static double Sample(long seed, double x, double y)
        {
            long x0 = (long)Math.Floor(x);
            long y0 = (long)Math.Floor(y);
            long x1 = x0 + 1;
            long y1 = y0 + 1;
            double tx = Fade(x - x0);
            double ty = Fade(y - y0);

            double bottom = Lerp(HashToSignedUnit(seed, x0, y0), HashToSignedUnit(seed, x1, y0), tx);
            double top = Lerp(HashToSignedUnit(seed, x0, y1), HashToSignedUnit(seed, x1, y1), tx);
            return Lerp(bottom, top, ty);
        }

        private static double HashToSignedUnit(long seed, long x, long y)
        {
            ulong value = unchecked((ulong)seed);
            value ^= unchecked((ulong)x) * 0x9E3779B185EBCA87UL;
            value ^= unchecked((ulong)y) * 0xC2B2AE3D27D4EB4FUL;
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            double unit = (value >> 11) * (1d / (1UL << 53));
            return (unit * 2d) - 1d;
        }

        private static double Fade(double value)
        {
            return value * value * value * ((value * ((value * 6d) - 15d)) + 10d);
        }

        private static double Lerp(double first, double second, double amount)
        {
            return first + ((second - first) * amount);
        }
    }
}
