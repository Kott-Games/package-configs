using System;
using System.Collections.Generic;
using System.Linq;
using Kott.Utilities.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Reads and writes config payloads:
    ///     <c>{ "formatVersion": 1, "templates": [ { "Type": "Item", "ID": "Item+Sword", … }, … ] }</c>.
    ///     A flat array of templates (the format of JSON template files) is read as well.
    /// </summary>
    public static class ConfigSerialization
    {
        /// <summary>The payload format version written, and the newest one read.</summary>
        public const int FormatVersion = 1;

        /// <summary>Name of the format version property.</summary>
        public const string FormatVersionProperty = "formatVersion";

        /// <summary>Name of the templates array property.</summary>
        public const string TemplatesProperty = "templates";

        /// <summary>Name of the type identifier property of a template.</summary>
        public const string TypeProperty = "Type";

        /// <summary>Name of the ID property of a template.</summary>
        public const string IdProperty = nameof(Template.ID);

        /// <summary>
        ///     Settings shared by all config serialization: Unity value types (vectors, colors, rects, bounds),
        ///     dictionaries as arrays, template asset references as addresses, default values omitted, no type names.
        /// </summary>
        public static JsonSerializerSettings CreateSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new List<JsonConverter>
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
                    new TemplateAssetReferenceConverter(),
                },
            };
        }

        /// <summary>Writes <paramref name="templates" /> (in the given order) as a payload.</summary>
        /// <exception cref="ConfigException">A template's type is not in <paramref name="typeMapping" />.</exception>
        public static JObject Serialize(ConfigTypeMapping typeMapping, IEnumerable<Template> templates)
        {
            if (typeMapping == null) throw new ArgumentNullException(nameof(typeMapping));
            if (templates == null) throw new ArgumentNullException(nameof(templates));

            Dictionary<Type, string> identifiers = typeMapping.ResolveTypes()
                .ToDictionary(pair => pair.Value, pair => pair.Key);

            JsonSerializerSettings settings = CreateSerializerSettings();
            settings.Converters.Add(new TemplateReferenceConverter());
            JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

            JArray jTemplates = new();
            foreach (Template template in templates)
            {
                Type type = template.GetType();
                if (!identifiers.TryGetValue(type, out string identifier))
                {
                    throw new ConfigException(
                        $"Template '{template.ID}': type {type.FullName} is not in the type mapping '{typeMapping.name}'.");
                }

                JObject jTemplate = JObject.FromObject(template, serializer);
                jTemplate.AddFirst(new JProperty(TypeProperty, identifier));

                // Unity object properties
                jTemplate.Remove("name");
                jTemplate.Remove("hideFlags");

                jTemplates.Add(jTemplate);
            }

            return new JObject
            {
                { FormatVersionProperty, FormatVersion },
                { TemplatesProperty, jTemplates },
            };
        }

        /// <summary>Reads a payload (or a flat template array) from JSON text.</summary>
        /// <exception cref="ConfigException">Invalid JSON, format version or template data; names the template.</exception>
        public static ConfigPayload Deserialize(ConfigTypeMapping typeMapping, string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonReaderException exception)
            {
                throw new ConfigException($"Payload is not valid JSON: {exception.Message}", exception);
            }

            return Deserialize(typeMapping, token);
        }

        /// <summary>Reads a payload (or a flat template array).</summary>
        /// <exception cref="ConfigException">Invalid format version or template data; names the template.</exception>
        public static ConfigPayload Deserialize(ConfigTypeMapping typeMapping, JToken payload)
        {
            if (typeMapping == null) throw new ArgumentNullException(nameof(typeMapping));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            JArray jTemplates = GetTemplates(payload);
            Dictionary<string, Type> types = typeMapping.ResolveTypes();

            JsonSerializerSettings settings = CreateSerializerSettings();
            TemplateReferenceConverter referenceConverter = new();
            settings.Converters.Add(referenceConverter);
            JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

            List<Template> templates = new();
            HashSet<string> ids = new();
            try
            {
                for (int i = 0; i < jTemplates.Count; i++)
                {
                    Template template = ReadTemplate(jTemplates[i], i, types, serializer, referenceConverter);
                    templates.Add(template);
                    if (!ids.Add(template.ID))
                    {
                        throw new ConfigException($"Duplicate template ID '{template.ID}' (entry #{i}).");
                    }
                }
            }
            catch
            {
                foreach (Template template in templates)
                {
                    ConfigObjectUtility.Destroy(template);
                }

                throw;
            }

            return new ConfigPayload(templates, referenceConverter.References);
        }

        private static JArray GetTemplates(JToken payload)
        {
            switch (payload)
            {
                case JArray array:
                    return array;
                case JObject jObject:
                    JToken version = jObject[FormatVersionProperty];
                    if (version == null || version.Type != JTokenType.Integer)
                    {
                        throw new ConfigException($"Payload has no integer '{FormatVersionProperty}'.");
                    }

                    int formatVersion = version.Value<int>();
                    if (formatVersion > FormatVersion)
                    {
                        throw new ConfigException(
                            $"Payload format version {formatVersion} is newer than the supported version " +
                            $"{FormatVersion}. Update the Configs package.");
                    }

                    if (formatVersion < 1)
                    {
                        throw new ConfigException($"Payload format version {formatVersion} is invalid.");
                    }

                    return jObject[TemplatesProperty] as JArray
                           ?? throw new ConfigException($"Payload has no '{TemplatesProperty}' array.");
                default:
                    throw new ConfigException($"Payload must be a JSON object or array, got {payload.Type}.");
            }
        }

        private static Template ReadTemplate(JToken token, int index, Dictionary<string, Type> types,
            JsonSerializer serializer, TemplateReferenceConverter referenceConverter)
        {
            if (token is not JObject jTemplate)
            {
                throw new ConfigException($"Template entry #{index} is not a JSON object ({token.Type}).");
            }

            string id = (jTemplate[IdProperty] as JValue)?.Value as string;
            if (string.IsNullOrEmpty(id))
            {
                throw new ConfigException($"Template entry #{index} has no '{IdProperty}'.");
            }

            string identifier = (jTemplate[TypeProperty] as JValue)?.Value as string;
            if (identifier == null)
            {
                throw new ConfigException($"Template '{id}' has no '{TypeProperty}'.");
            }

            if (!types.TryGetValue(identifier, out Type type))
            {
                throw new ConfigException(
                    $"Template '{id}': unknown type '{identifier}'. Known types: {string.Join(", ", types.Keys)}. " +
                    "Was the type renamed? Pin its identifier with [TemplateType].");
            }

            if (type.IsAbstract)
            {
                throw new ConfigException($"Template '{id}': type '{identifier}' ({type.FullName}) is abstract.");
            }

            Template template = (Template)ScriptableObject.CreateInstance(type);
            template.name = id;
            referenceConverter.Owner = template;
            try
            {
                serializer.Populate(jTemplate.CreateReader(), template);
            }
            catch (JsonException exception)
            {
                ConfigObjectUtility.Destroy(template);
                throw new ConfigException($"Template '{id}' ({identifier}): {exception.Message}", exception);
            }

            return template;
        }
    }
}
