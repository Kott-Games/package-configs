using Newtonsoft.Json;
using System;

namespace Kott.Configs
{
    public class TemplateReferenceConverter : JsonConverter<ITemplateReference>
    {
        public override ITemplateReference ReadJson(JsonReader reader, Type objectType, ITemplateReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, ITemplateReference value, JsonSerializer serializer)
        {
            string id = value.Template ? value.Template.ID : null;
            writer.WriteValue(id);
        }
    }
}
