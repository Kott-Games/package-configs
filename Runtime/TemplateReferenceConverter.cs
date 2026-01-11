using Newtonsoft.Json;
using System;

namespace Kott.Configs
{
    public class TemplateReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Template).IsAssignableFrom(objectType) || typeof(ITemplateReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Template template)
            {
                WriteTemplate(writer, template);
            }
            else if (value is ITemplateReference templateReference)
            {
                WriteTemplate(writer, templateReference.Template);
            }
        }

        private static void WriteTemplate(JsonWriter writer, Template template)
        {
            string id = template ? template.ID : null;
            writer.WriteValue(id);
        }
    }
}
