using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Kott.Configs
{
    public delegate void TemplateAssetReadyDelegate<TAsset>(TAsset asset);

    [Serializable]
    public class TemplateAssetReference<TAsset> : ITemplateAssetReference
        where TAsset : UnityEngine.Object
    {
#if UNITY_EDITOR
        [SerializeField] private TAsset _asset;
        [SerializeField] private string _address;

        UnityEngine.Object ITemplateAssetReference.Asset => _asset;

        public string Address
        {
            get => _address;
            set => _address = value;
        }
#else
        public string Address { get; set; }
#endif

        public AsyncOperationHandle<TAsset> LoadAssetAsync(TemplateAssetReadyDelegate<TAsset> assetReady)
        {
            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(Address);

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
