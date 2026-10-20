using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kott.Configs
{
    /// <summary>
    ///     Writes template references as the target's ID. Reading creates empty references and records them as
    ///     <see cref="PendingReference" />s of <see cref="Owner" />, to be resolved after merging.
    /// </summary>
    internal sealed class TemplateReferenceConverter : JsonConverter
    {
        public List<PendingReference> References { get; } = new();

        /// <summary>The template being read; owner of the references read meanwhile.</summary>
        public Template Owner { get; set; }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ITemplateReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Null && reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"Expected a template ID string for {objectType.Name}, got {reader.TokenType} at '{reader.Path}'.");
            }

            ITemplateReference reference = (ITemplateReference)Activator.CreateInstance(objectType);
            string id = reader.Value as string;
            if (!string.IsNullOrEmpty(id))
            {
                References.Add(new PendingReference(Owner, reference, id));
            }

            return reference;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Template template = ((ITemplateReference)value).Template;
            writer.WriteValue(template ? template.ID : null);
        }
    }
}
