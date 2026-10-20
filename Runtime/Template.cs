using Newtonsoft.Json;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Base class of all config templates: one asset per template, its ID derived from the group and asset name when
    ///     publishing (<see cref="TemplateIdUtility.CreateId" />). At runtime, templates are created from payloads by a
    ///     <see cref="ConfigProvider" />.
    /// </summary>
    public abstract class Template : ScriptableObject
    {
        /// <summary>Unique ID of the template within its config set, e.g. <c>"Item+Sword"</c>.</summary>
        [JsonProperty(Order = -9001)]
        public string ID;
    }
}
