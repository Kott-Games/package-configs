using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Kott.Configs
{
    [Serializable]
    public class TemplateAssetReference<TAsset> : ITemplateAssetReference
        where TAsset : UnityEngine.Object
    {
        [SerializeField]
        private string _address;

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
