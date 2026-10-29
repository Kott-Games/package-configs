using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kott.Configs.Attributes;

namespace Kott.Configs
{
    /// <summary>
    ///     Lookup over a fixed set of templates: by ID, by enum value and as typed collections that include subtypes.
    ///     Does not own the templates.
    /// </summary>
    public sealed class TemplateCatalog : IConfigProvider
    {
        private readonly Dictionary<string, Template> _templates = new();
        private readonly Dictionary<Type, IList> _collections = new();
        private readonly Dictionary<Type, EnumLookup> _enumLookups = new();

        /// <param name="templates">The templates. IDs must be unique.</param>
        /// <param name="knownTypes">
        ///     Template types that get a (possibly empty) collection, e.g. the types of a <see cref="ConfigTypeMapping" />.
        ///     The types of <paramref name="templates" />, their base types and <see cref="Template" /> are always known.
        /// </param>
        /// <exception cref="ConfigException">An ID is missing or not unique.</exception>
        public TemplateCatalog(IEnumerable<Template> templates, IEnumerable<Type> knownTypes = null)
        {
            if (templates == null) throw new ArgumentNullException(nameof(templates));

            Dictionary<Type, List<Template>> collections = new() { { typeof(Template), new List<Template>() } };
            if (knownTypes != null)
            {
                foreach (Type type in knownTypes)
                {
                    if (typeof(Template).IsAssignableFrom(type) && !collections.ContainsKey(type))
                    {
                        collections.Add(type, new List<Template>());
                    }
                }
            }

            foreach (Template template in templates)
            {
                if (!template) throw new ArgumentException("Templates contain null.", nameof(templates));
                if (string.IsNullOrEmpty(template.ID))
                {
                    throw new ConfigException($"Template '{template.name}' ({template.GetType().Name}) has no ID.");
                }

                if (!_templates.TryAdd(template.ID, template))
                {
                    throw new ConfigException($"Duplicate template ID '{template.ID}'.");
                }

                for (Type type = template.GetType(); type != typeof(Template).BaseType; type = type.BaseType)
                {
                    if (!collections.TryGetValue(type, out List<Template> collection))
                    {
                        collection = new List<Template>();
                        collections.Add(type, collection);
                    }

                    collection.Add(template);
                }
            }

            foreach ((Type type, List<Template> templatesOfType) in collections)
            {
                Array array = Array.CreateInstance(type, templatesOfType.Count);
                for (int i = 0; i < templatesOfType.Count; i++)
                {
                    array.SetValue(templatesOfType[i], i);
                }

                _collections.Add(type, array);
            }
        }

        /// <summary>All templates by ID.</summary>
        public IReadOnlyDictionary<string, Template> Templates => _templates;

        /// <inheritdoc />
        public T GetTemplate<T>(string id) where T : Template
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (!_templates.TryGetValue(id, out Template template))
            {
                throw new KeyNotFoundException($"No template with ID '{id}'.");
            }

            return template as T ?? throw new InvalidCastException(
                $"Template '{id}' is a {template.GetType().Name}, not a {typeof(T).Name}.");
        }

        /// <inheritdoc />
        public bool TryGetTemplate<T>(string id, out T template) where T : Template
        {
            template = id != null && _templates.TryGetValue(id, out Template found) ? found as T : null;
            return template != null;
        }

        /// <inheritdoc />
        public T GetTemplate<T>(Enum value) where T : Template
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Dictionary<Enum, Template> lookup = GetEnumLookup(typeof(T), value.GetType());
            if (!lookup.TryGetValue(value, out Template template))
            {
                throw new KeyNotFoundException(
                    $"No template for {value.GetType().Name}.{value} ({typeof(T).Name} ID " +
                    $"'{TemplateIdUtility.CreateEnumId(typeof(T), value)}').");
            }

            return (T)template;
        }

        /// <inheritdoc />
        public IReadOnlyList<T> GetCollection<T>() where T : Template
        {
            if (!_collections.TryGetValue(typeof(T), out IList collection))
            {
                throw new KeyNotFoundException(
                    $"{typeof(T).Name} is not a known template type: no templates of it, and not in the type mapping.");
            }

            return (IReadOnlyList<T>)collection;
        }

        private Dictionary<Enum, Template> GetEnumLookup(Type templateType, Type enumType)
        {
            if (!_enumLookups.TryGetValue(templateType, out EnumLookup lookup))
            {
                lookup = CreateEnumLookup(templateType);
                _enumLookups.Add(templateType, lookup);
            }

            if (lookup.EnumType != enumType)
            {
                throw new ArgumentException($"{templateType.Name} has no [TemplateEnum(typeof({enumType.Name}))].");
            }

            return lookup.Templates;
        }

        private EnumLookup CreateEnumLookup(Type templateType)
        {
            TemplateEnumAttribute attribute = templateType.GetCustomAttribute<TemplateEnumAttribute>(false);
            EnumLookup lookup = new(attribute?.EnumType);
            if (lookup.EnumType == null) return lookup;

            foreach (Enum value in Enum.GetValues(lookup.EnumType))
            {
                if (_templates.TryGetValue(TemplateIdUtility.CreateEnumId(templateType, value),
                        out Template template) &&
                    templateType.IsInstanceOfType(template))
                {
                    lookup.Templates[value] = template;
                }
            }

            return lookup;
        }

        private sealed class EnumLookup
        {
            public EnumLookup(Type enumType)
            {
                EnumType = enumType;
            }

            public Type EnumType { get; }
            public Dictionary<Enum, Template> Templates { get; } = new();
        }
    }
}
