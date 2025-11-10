using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Kott.Configs.Converters
{
    public class BoundsConverter : JsonConverter<Bounds>
    {
        public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Bounds value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("center");
            serializer.Serialize(writer, value.center, typeof(Vector3));
            writer.WritePropertyName("extents");
            serializer.Serialize(writer, value.extents, typeof(Vector3));
            writer.WriteEndObject();
        }
    }
}
