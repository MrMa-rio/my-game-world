using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public interface IAssetRegistry<TAsset>
    {
        AssetCatalogVersion Version { get; }

        bool TryResolve(AssetId assetId, out TAsset asset);
    }
}
