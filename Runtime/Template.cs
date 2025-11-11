using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Kott.Configs
{
    public interface ITemplate
    {
        string ID { get; }

        AsyncOperationHandle LoadAssets();
    }

    // TODO create config package (needs to use assembly in type key)
    public abstract class Template : ScriptableObject, ITemplate
    {
        [JsonProperty(Order = -9001)]
        public string ID;

        string ITemplate.ID => ID;

        public string[] AssetAddressesCombined;

        public AsyncOperationHandle LoadAssets()
        {
            return Addressables.LoadAssetsAsync<UnityEngine.Object>(
                AssetAddressesCombined, null, Addressables.MergeMode.Union);
        }
    }

    public static class TemplateExtensions
    {
        public static string FormatID(this Template template)
        {
            if (template == null) return "[NULL]";
            return $"[{template.ID}]";
        }
    }
}
