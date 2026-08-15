namespace MyGameWorld.Shared.Procedural
{
    public static class AssetCompatibilityEvaluator
    {
        public static bool AreCompatible(AssetDescriptor first, AssetDescriptor second)
        {
            return first.Compatibility.Accepts(second.Traits)
                && second.Compatibility.Accepts(first.Traits);
        }
    }
}
