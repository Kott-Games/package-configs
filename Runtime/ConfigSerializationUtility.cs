using System;
using System.Collections.Generic;
using System.Linq;
using Kott.Utilities.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kott.Configs
{
    public static class ConfigSerializationUtility
    {
        public static JArray Serialize(ConfigTypeMapping typeMapping, List<Template> templates)
        {
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new List<JsonConverter>()
                {
                    new Vector2Converter(),
                    new Vector2IntConverter(),
                    new Vector3Converter(),
                    new Vector3IntConverter(),
                    new Vector4Converter(),
                    new QuaternionConverter(),
                    new ColorConverter(),
                    new RectConverter(),
                    new RectIntConverter(),
                    new BoundsConverter(),
                    new BoundsIntConverter(),
                    new DictionaryArrayConverter(),
                    new TemplateReferenceConverter(),
                    new TemplateAssetReferenceConverter(),
                },
            };

            JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
            
            Dictionary<Type, string> typeLookup = typeMapping.TypeGroups
                .ToDictionary(group => Type.GetType(group.TypeString), group => group.TypeIdentifier);

            JArray jTemplates = new JArray();
            foreach (Template template in templates)
            {
                JObject jTemplate = JObject.FromObject(template, serializer);
                
                Type type = template.GetType();
                string typeIdentifier = typeLookup[type];
                jTemplate.AddFirst(new JProperty("Type", typeIdentifier));
                
                // remove unnecessary unity object properties
                jTemplate.Remove("name");
                jTemplate.Remove("hideFlags");
                
                jTemplates.Add(jTemplate);
            }
            
            return jTemplates;
        }
        
        public static List<Template> Deserialize(ConfigTypeMapping typeMapping, JArray jTemplates)
        {
            TemplateReferenceConverter templateReferenceConverter = new TemplateReferenceConverter();
            TemplateAssetReferenceConverter templateAssetReferenceConverter = new TemplateAssetReferenceConverter();
            
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new List<JsonConverter>()
                {
                    new Vector2Converter(),
                    new Vector2IntConverter(),
                    new Vector3Converter(),
                    new Vector3IntConverter(),
                    new Vector4Converter(),
                    new QuaternionConverter(),
                    new ColorConverter(),
                    new RectConverter(),
                    new RectIntConverter(),
                    new BoundsConverter(),
                    new BoundsIntConverter(),
                    new DictionaryArrayConverter(),
                    templateReferenceConverter,
                    templateAssetReferenceConverter,
                },
            };

            JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

            Dictionary<string, Template> templateLookup = new();
            Dictionary<string, Type> typeLookup = typeMapping.TypeGroups
                .ToDictionary(group => group.TypeIdentifier, group => Type.GetType(group.TypeString));

            foreach (JObject jTemplate in jTemplates)
            {
                string typeIdentifier = jTemplate.Value<string>("Type");
                Type type = typeLookup[typeIdentifier];
                
                Template template = (Template)ScriptableObject.CreateInstance(type);
                serializer.Populate(jTemplate.CreateReader(), template);
                
                templateLookup.Add(template.ID, template);
            }
            
            foreach ((string id, List<ITemplateReference> references) in templateReferenceConverter.References)
            {
                Template template = templateLookup.GetValueOrDefault(id);
                foreach (ITemplateReference reference in references)
                {
                    reference.Template = template;
                }
            }

            return templateLookup.Values.ToList();
        }
    }
}