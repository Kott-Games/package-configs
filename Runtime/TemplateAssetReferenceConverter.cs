using Newtonsoft.Json;
using System;

namespace Kott.Configs
{
    public class TemplateAssetReferenceConverter : JsonConverter<ITemplateAssetReference>
    {
        public override ITemplateAssetReference ReadJson(JsonReader reader, Type objectType, ITemplateAssetReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, ITemplateAssetReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Address);
        }
    }
}
