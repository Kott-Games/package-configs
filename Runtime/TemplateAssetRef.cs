using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Kott.Configs
{
    public interface ITemplateAssetRef
    {
        UnityEngine.Object Asset { get; }

        string Address { get; set; }
    }

    public delegate void TemplateAssetReadyDelegate<TAsset>(TAsset asset);

    [Serializable]
    public class TemplateAssetRef<T> : ITemplateAssetRef
        where T : UnityEngine.Object
    {
        [SerializeField] private T _asset;
        [SerializeField] private string _address;

        UnityEngine.Object ITemplateAssetRef.Asset { get => _asset; }
        string ITemplateAssetRef.Address { get => _address; set => _address = value; }

        public AsyncOperationHandle<T> LoadAssetAsync(TemplateAssetReadyDelegate<T> assetReady)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(_address);

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
