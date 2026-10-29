using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Creates templates from the JSON objects of a payload. Template references stay empty and are recorded in
    ///     <see cref="References" />, to be resolved once all templates exist.
    /// </summary>
    internal sealed class TemplateReader
    {
        private readonly IReadOnlyDictionary<string, Type> _types;
        private readonly bool _strict;
        private readonly TemplateReferenceConverter _referenceConverter = new();
        private readonly JsonSerializer _serializer;

        /// <param name="types">Type identifier → template type.</param>
        /// <param name="strict">
        ///     Fail on JSON properties that match no field (typos in authored JSON). Payloads are read leniently, so an
        ///     older build ignores fields added later.
        /// </param>
        public TemplateReader(IReadOnlyDictionary<string, Type> types, bool strict = false)
        {
            _types = types ?? throw new ArgumentNullException(nameof(types));
            _strict = strict;

            JsonSerializerSettings settings = ConfigSerialization.CreateSerializerSettings();
            settings.Converters.Add(_referenceConverter);
            if (strict) settings.MissingMemberHandling = MissingMemberHandling.Error;
            _serializer = JsonSerializer.CreateDefault(settings);
        }

        /// <summary>The template references read so far.</summary>
        public List<PendingReference> References => _referenceConverter.References;

        /// <summary>Creates the template described by <paramref name="token" />, entry <paramref name="index" /> of its payload.</summary>
        /// <exception cref="ConfigException">Invalid entry; names the template (or the entry, without ID).</exception>
        public Template Read(JToken token, int index)
        {
            if (token is not JObject jTemplate)
            {
                throw new ConfigException($"Template entry #{index} is not a JSON object ({token.Type}).");
            }

            string id = (jTemplate[ConfigSerialization.IdProperty] as JValue)?.Value as string;
            if (string.IsNullOrEmpty(id))
            {
                throw new ConfigException($"Template entry #{index} has no '{ConfigSerialization.IdProperty}'.");
            }

            string identifier = (jTemplate[ConfigSerialization.TypeProperty] as JValue)?.Value as string;
            if (identifier == null)
            {
                throw new ConfigException($"Template '{id}' has no '{ConfigSerialization.TypeProperty}'.");
            }

            if (!_types.TryGetValue(identifier, out Type type))
            {
                throw new ConfigException(
                    $"Template '{id}': unknown type '{identifier}'. Known types: {string.Join(", ", _types.Keys)}. " +
                    "Was the type renamed? Pin its identifier with [TemplateType].");
            }

            if (type.IsAbstract)
            {
                throw new ConfigException($"Template '{id}': type '{identifier}' ({type.FullName}) is abstract.");
            }

            JObject data = jTemplate;
            if (_strict)
            {
                // the type identifier is no field
                data = (JObject)jTemplate.DeepClone();
                data.Remove(ConfigSerialization.TypeProperty);
            }

            Template template = (Template)ScriptableObject.CreateInstance(type);
            template.name = id;
            _referenceConverter.Owner = template;
            try
            {
                _serializer.Populate(data.CreateReader(), template);
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
