using System;
using Newtonsoft.Json;

namespace Kott.Configs
{
    /// <summary>Writes and reads template asset references as their Addressables address.</summary>
    internal sealed class TemplateAssetReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ITemplateAssetReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Null && reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"Expected an address string for {objectType.Name}, got {reader.TokenType} at '{reader.Path}'.");
            }

            ITemplateAssetReference reference = (ITemplateAssetReference)Activator.CreateInstance(objectType);
            reference.Address = reader.Value as string;
            return reference;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Unity serializes a cleared address as "": both mean no asset
            string address = ((ITemplateAssetReference)value).Address;
            if (string.IsNullOrEmpty(address)) writer.WriteNull();
            else writer.WriteValue(address);
        }
    }
}
