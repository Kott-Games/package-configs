using Newtonsoft.Json;
using UnityEngine;

namespace Kott.Configs
{
    public abstract class Template : ScriptableObject
    {
        [JsonProperty(Order = -9001)]
        public string ID;
    }
}
