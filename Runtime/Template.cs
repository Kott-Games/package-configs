using Kott.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Kott.Configs
{
    public interface ITemplate
    {
        string ID { get; }

        AsyncOperationHandle LoadAssets();
    }

    public abstract class Template : ScriptableObject, ITemplate
    {
        [JsonProperty(Order = -9001)]
        public string ID;

        string ITemplate.ID => ID;

        public string[] AssetAddressesCombined;

        public AsyncOperationHandle LoadAssets()
        {
            return AddressablesUtility.LoadAssets(AssetAddressesCombined);
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
