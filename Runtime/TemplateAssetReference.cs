using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Kott.Configs
{
    /// <summary>
    ///     A reference from a template to an Addressables asset. In the editor you assign the asset; push writes its
    ///     address (validation reports assets that aren't addressable). Payloads only carry the address.
    /// </summary>
    /// <typeparam name="TAsset">The asset type, e.g. <see cref="Sprite" /> or <see cref="GameObject" />.</typeparam>
    [Serializable]
    public class TemplateAssetReference<TAsset> : ITemplateAssetReference
        where TAsset : UnityEngine.Object
    {
        [SerializeField]
        private string _address;

        /// <summary>The asset's Addressables address; empty if no asset is referenced.</summary>
        public string Address => _address;

        string ITemplateAssetReference.Address
        {
            get => _address;
            set => _address = value;
        }

#if UNITY_EDITOR
        [SerializeField]
        private TAsset _asset;

        UnityEngine.Object ITemplateAssetReference.Asset
        {
            get => _asset;
            set => _asset = value as TAsset;
        }
#endif

        /// <summary>
        ///     Loads the asset through <see cref="Addressables.LoadAssetAsync{TObject}(object)" /> and calls
        ///     <paramref name="assetReady" /> with it (immediately if it is already loaded). Release the returned
        ///     handle when you no longer need the asset.
        /// </summary>
        public AsyncOperationHandle<TAsset> LoadAssetAsync(Action<TAsset> assetReady)
        {
            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(_address);

            if (handle.IsDone)
            {
                assetReady(handle.Result);
            }
            else
            {
                handle.Completed += h => assetReady(h.Result);
            }

            return handle;
        }
    }
}
