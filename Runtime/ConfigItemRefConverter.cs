using Newtonsoft.Json;
using System;

namespace Kott.Configs.Converters
{
    public class ConfigItemRefConverter : JsonConverter<ITemplateRef>
    {
        public override ITemplateRef ReadJson(JsonReader reader, Type objectType, ITemplateRef existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, ITemplateRef value, JsonSerializer serializer)
        {
            string sValue = value.Asset != null ? sValue = value.Asset.ID : "";
            serializer.Serialize(writer, sValue, typeof(string));
        }
    }
}
