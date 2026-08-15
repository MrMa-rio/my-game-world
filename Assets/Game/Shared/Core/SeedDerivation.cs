namespace MyGameWorld.Shared.Core
{
    public static class SeedDerivation
    {
        public static long Derive(long parentSeed, uint scope, long localId)
        {
            ulong value = unchecked((ulong)parentSeed);
            value = Mix(value ^ scope);
            value = Mix(value ^ unchecked((ulong)localId));
            return unchecked((long)value);
        }

        private static ulong Mix(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
