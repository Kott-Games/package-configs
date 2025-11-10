using Newtonsoft.Json;
using UnityEngine;

namespace Kott.Configs
{
    public interface ITemplate
    {
        string ID { get; }
    }

    // TODO create config package (needs to use assembly in type key)
    public abstract class Template : ScriptableObject, ITemplate
    {
        [JsonProperty(Order = -9001)]
        public string ID;

        string ITemplate.ID => ID;
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
