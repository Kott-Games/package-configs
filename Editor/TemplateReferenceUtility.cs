using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Kott.Configs
{
    public static class TemplateReferenceUtility
    {
        private class OtherConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return
                    objectType == typeof(Vector2) ||
                    objectType == typeof(Vector2Int) ||
                    objectType == typeof(Vector3) ||
                    objectType == typeof(Vector3Int) ||
                    objectType == typeof(Vector4) ||
                    objectType == typeof(Quaternion) ||
                    objectType == typeof(Color) ||
                    objectType == typeof(Rect) ||
                    objectType == typeof(RectInt) ||
                    objectType == typeof(Bounds) ||
                    objectType == typeof(BoundsInt);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(0);
            }
        }

        private class CollectConverter<T> : JsonConverter<T>
        {
            private readonly ICollection<T> _templateRefs;

            public CollectConverter(ICollection<T> templatesRefs)
            {
                _templateRefs = templatesRefs;
            }

            public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
            {
                _templateRefs.Add(value);
                writer.WriteValue(0);
            }
        }

        public static void GetTemplateRefs(Template template,
            ICollection<ITemplateReference> templateReferences,
            ICollection<ITemplateAssetReference> templateAssetReferences)
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(new OtherConverter());
            serializer.Converters.Add(new CollectConverter<ITemplateReference>(templateReferences));
            serializer.Converters.Add(new CollectConverter<ITemplateAssetReference>(templateAssetReferences));

            JToken token = JToken.FromObject(template, serializer);
        }
    }
}
