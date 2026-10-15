using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kott.Configs
{
    public abstract class StringReferenceConverter<T> : JsonConverter<T>
    {
        public Dictionary<string, List<T>> References { get; } = new();

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            string id = reader.Value as string;
            existingValue = CreateInstance(objectType, id);

            if (!string.IsNullOrEmpty(id))
            {
                if (!References.TryGetValue(id, out List<T> references))
                {
                    references = new List<T>();
                    References.Add(id, references);
                }

                references.Add(existingValue);
            }

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            string id = GetInstanceId(value);

            if (!string.IsNullOrEmpty(id))
            {
                if (!References.TryGetValue(id, out List<T> references))
                {
                    references = new List<T>();
                    References.Add(id, references);
                }

                references.Add(value);
            }

            writer.WriteValue(id);
        }

        protected abstract string GetInstanceId(T instance);
        protected abstract T CreateInstance(Type type, string id);
    }
}
