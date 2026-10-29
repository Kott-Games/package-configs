using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kott.Configs.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kott.Configs
{
    /// <summary>A JSON template file: a flat array of templates.</summary>
    internal sealed class JsonTemplateFile
    {
        public JsonTemplateFile(string path, string text, Object asset = null)
        {
            Path = path;
            Text = text;
            Asset = asset;
        }

        /// <summary>Asset path, used in messages.</summary>
        public string Path { get; }

        public string Text { get; }

        /// <summary>The file's asset, context of its issues. May be <c>null</c>.</summary>
        public Object Asset { get; }
    }

    /// <summary>Resolves the Addressables addresses of assets referenced by <see cref="TemplateAssetReference{TAsset}" />.</summary>
    internal interface IAssetAddressResolver
    {
        /// <summary>The address of <paramref name="asset" />, or <c>null</c> if it is not addressable.</summary>
        string GetAddress(Object asset);

        /// <summary>Makes <paramref name="asset" /> addressable.</summary>
        void MakeAddressable(Object asset);
    }

    /// <summary>The outcome of building a <see cref="ConfigSet" />: issues and, without errors, the payload.</summary>
    public sealed class ConfigBuildResult
    {
        internal ConfigBuildResult(List<ConfigIssue> issues, JObject payload, List<Type> templateTypes,
            int templateCount, List<Object> modifiedAssets)
        {
            Issues = issues;
            Payload = payload;
            TemplateTypes = templateTypes;
            TemplateCount = templateCount;
            ModifiedAssets = modifiedAssets;
        }

        /// <summary>All problems found, errors first.</summary>
        public IReadOnlyList<ConfigIssue> Issues { get; }

        /// <summary>Whether there are errors (then there is no <see cref="Payload" />).</summary>
        public bool HasErrors => Issues.Any(issue => issue.Severity == ConfigIssueSeverity.Error);

        /// <summary>The baked payload; <c>null</c> if there are errors.</summary>
        public JObject Payload { get; }

        /// <summary>The template types in use (without base types), ordered by type identifier.</summary>
        public IReadOnlyList<Type> TemplateTypes { get; }

        /// <summary>Number of templates in the set.</summary>
        public int TemplateCount { get; }

        /// <summary>Template assets whose derived ID or asset addresses were updated (not saved yet).</summary>
        internal IReadOnlyList<Object> ModifiedAssets { get; }
    }

    /// <summary>
    ///     Resolves, validates and bakes the templates of a config set. Asset and JSON templates become template
    ///     instances (assets as they are, JSON through the runtime reader), references are resolved by ID, and all are
    ///     written with <see cref="ConfigSerialization" />, ordered by ID, so identical data gives identical entries.
    /// </summary>
    internal sealed class ConfigBuild
    {
        private readonly IAssetAddressResolver _addresses;
        private readonly Dictionary<string, Type> _types = new();
        private readonly Dictionary<string, List<Type>> _ambiguousTypes = new();

        private readonly List<ConfigIssue> _issues = new();
        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
        private readonly Dictionary<Template, Entry> _entriesByTemplate = new();
        private readonly HashSet<Template> _assetTemplates = new();
        private readonly List<Template> _created = new();
        private readonly List<Object> _modified = new();

        private ConfigBuild(IAssetAddressResolver addresses, IEnumerable<Type> templateTypes)
        {
            _addresses = addresses;

            foreach (IGrouping<string, Type> group in templateTypes.GroupBy(TemplateIdUtility.GetTypeIdentifier))
            {
                List<Type> types = group.ToList();
                if (types.Count == 1) _types.Add(group.Key, types[0]);
                else _ambiguousTypes.Add(group.Key, types);
            }
        }

        /// <param name="assetTemplates">Template assets of the set.</param>
        /// <param name="jsonFiles">JSON template files of the set.</param>
        /// <param name="addresses">Resolves addresses of asset references.</param>
        /// <param name="templateTypes">Template types JSON entries may use. Default: all in the project.</param>
        public static ConfigBuildResult Run(IEnumerable<Template> assetTemplates,
            IEnumerable<JsonTemplateFile> jsonFiles,
            IAssetAddressResolver addresses, IEnumerable<Type> templateTypes = null)
        {
            templateTypes ??= TypeCache.GetTypesDerivedFrom<Template>().Where(type => !type.ContainsGenericParameters);
            ConfigBuild build = new(addresses, templateTypes);
            return build.Run(assetTemplates.Where(template => template).Distinct().ToList(), jsonFiles.ToList());
        }

        private ConfigBuildResult Run(List<Template> assetTemplates, List<JsonTemplateFile> jsonFiles)
        {
            try
            {
                _assetTemplates.UnionWith(assetTemplates);
                foreach (Template template in assetTemplates)
                {
                    AddAsset(template);
                }

                TemplateReader reader = new(_types, strict: true);
                foreach (JsonTemplateFile file in jsonFiles)
                {
                    AddJsonFile(file, reader);
                }

                ResolveReferences(reader.References);

                List<Type> usedTypes = _entries.Values
                    .Select(entry => entry.Template.GetType())
                    .Distinct()
                    .OrderBy(TemplateIdUtility.GetTypeIdentifier, StringComparer.Ordinal)
                    .ThenBy(type => type.FullName, StringComparer.Ordinal)
                    .ToList();
                List<Type> mappedTypes = WithBaseTypes(usedTypes);
                CheckTypes(mappedTypes);

                JObject payload = _issues.Any(issue => issue.Severity == ConfigIssueSeverity.Error)
                    ? null
                    : Bake(mappedTypes);

                List<ConfigIssue> issues = _issues.OrderBy(issue => issue.Severity).ToList();
                return new ConfigBuildResult(issues, payload, usedTypes, _entries.Count, _modified);
            }
            finally
            {
                foreach (Template template in _created)
                {
                    Object.DestroyImmediate(template);
                }
            }
        }

        private void AddAsset(Template template)
        {
            if (AssetDatabase.IsSubAsset(template))
            {
                Error($"Template '{template.name}' is a sub-asset of '{AssetDatabase.GetAssetPath(template)}'. " +
                      "Templates must be main assets (one asset per template).", template);
                return;
            }

            string id = TemplateIdUtility.CreateId(template.GetType(), template.name);
            if (template.ID != id)
            {
                template.ID = id;
                MarkModified(template);
            }

            List<(object Reference, string Path)> references;
            try
            {
                references = CollectReferences(template);
            }
            catch (JsonException exception)
            {
                Error($"Template '{id}' can't be serialized: {exception.Message}", template);
                return;
            }

            foreach ((object reference, string path) in references)
            {
                switch (reference)
                {
                    case ITemplateReference templateReference:
                        CheckAssetReference(template, templateReference, path);
                        break;
                    case ITemplateAssetReference assetReference:
                        UpdateAddress(template, assetReference, path);
                        break;
                }
            }

            string assetPath = AssetDatabase.GetAssetPath(template);
            AddEntry(new Entry(template, template, 0, string.IsNullOrEmpty(assetPath) ? template.name : assetPath));
        }

        private void CheckAssetReference(Template owner, ITemplateReference reference, string path)
        {
            Template target = reference.Template;
            if (!target || _assetTemplates.Contains(target)) return;

            string targetPath = AssetDatabase.GetAssetPath(target);
            Error(
                $"Template '{owner.ID}' references '{(string.IsNullOrEmpty(targetPath) ? target.name : targetPath)}' " +
                $"({path}), which is not in this config set. References must stay inside the set.", owner);
        }

        private void UpdateAddress(Template owner, ITemplateAssetReference reference, string path)
        {
            Object asset = reference.Asset;
            if (!asset)
            {
                // a cleared asset must not keep shipping its old address
                if (!string.IsNullOrEmpty(reference.Address))
                {
                    reference.Address = null;
                    MarkModified(owner);
                }

                return;
            }

            string address = _addresses.GetAddress(asset);
            if (address == null)
            {
                Error($"Template '{owner.ID}' references the asset '{asset.name}' ({path}), which is not addressable.",
                    owner, fix: () => _addresses.MakeAddressable(asset));
                return;
            }

            if (reference.Address != address)
            {
                reference.Address = address;
                MarkModified(owner);
            }
        }

        private void AddJsonFile(JsonTemplateFile file, TemplateReader reader)
        {
            JToken root;
            try
            {
                root = JToken.Parse(file.Text);
            }
            catch (JsonReaderException exception)
            {
                Error($"{file.Path}: invalid JSON: {exception.Message}", file.Asset, exception.LineNumber);
                return;
            }

            if (root is not JArray entries)
            {
                Error($"{file.Path}: a JSON template file must be a flat array of templates, got {root.Type}.",
                    file.Asset, 1);
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                JToken token = entries[i];
                int line = ((IJsonLineInfo)token).LineNumber;

                string identifier = (token[ConfigSerialization.TypeProperty] as JValue)?.Value as string;
                if (identifier != null && _ambiguousTypes.TryGetValue(identifier, out List<Type> candidates))
                {
                    Error($"{file.Path}: entry #{i} has the type '{identifier}', which is the identifier of " +
                          $"{string.Join(" and ", candidates.Select(type => type.FullName))}. Pin distinct " +
                          "identifiers with [TemplateType].", file.Asset, line);
                    continue;
                }

                Template template;
                try
                {
                    template = reader.Read(token, i);
                }
                catch (ConfigException exception)
                {
                    Error($"{file.Path}: {exception.Message}", file.Asset, line);
                    continue;
                }

                _created.Add(template);
                AddEntry(new Entry(template, file.Asset, line, $"{file.Path} (line {line})"));
            }
        }

        private void AddEntry(Entry entry)
        {
            string id = entry.Template.ID;
            if (_entries.TryGetValue(id, out Entry existing))
            {
                Error($"Duplicate template ID '{id}': {existing.Source} and {entry.Source}.", entry.Context,
                    entry.Line);
                return;
            }

            _entries.Add(id, entry);
            _entriesByTemplate.Add(entry.Template, entry);
        }

        private void ResolveReferences(IEnumerable<PendingReference> references)
        {
            foreach (PendingReference pending in references)
            {
                // not in the set (e.g. a duplicate ID)
                if (!_entriesByTemplate.TryGetValue(pending.Owner, out Entry owner)) continue;

                if (!_entries.TryGetValue(pending.TargetId, out Entry target))
                {
                    Error($"Template '{pending.Owner.ID}' references '{pending.TargetId}', which is not in this " +
                          "config set.", owner.Context, owner.Line);
                    continue;
                }

                pending.Reference.Template = target.Template;
                if (pending.Reference.Template != target.Template)
                {
                    Error($"Template '{pending.Owner.ID}' references '{pending.TargetId}', whose type " +
                          $"{target.Template.GetType().Name} doesn't match the reference.", owner.Context, owner.Line);
                }
            }
        }

        private void CheckTypes(List<Type> mappedTypes)
        {
            foreach (IGrouping<string, Type> group in mappedTypes.GroupBy(TemplateIdUtility.GetTypeIdentifier))
            {
                if (group.Count() < 2) continue;
                Error($"Type identifier '{group.Key}' is used by " +
                      $"{string.Join(" and ", group.Select(type => type.FullName))}. Pin distinct identifiers with " +
                      "[TemplateType].");
            }

            foreach (Type type in mappedTypes)
            {
                Type enumType = type.GetCustomAttribute<TemplateEnumAttribute>(false)?.EnumType;
                if (enumType == null || !enumType.IsEnum) continue;

                foreach (Enum value in Enum.GetValues(enumType))
                {
                    string id = TemplateIdUtility.CreateEnumId(type, value);
                    if (_entries.TryGetValue(id, out Entry entry) && type.IsInstanceOfType(entry.Template)) continue;

                    Warning($"{type.Name} has no template for {enumType.Name}.{value} (ID '{id}').");
                }
            }
        }

        private JObject Bake(List<Type> mappedTypes)
        {
            List<Template> templates = _entries.Values
                .Select(entry => entry.Template)
                .OrderBy(template => template.ID, StringComparer.Ordinal)
                .ToList();
            Dictionary<Type, string> identifiers = mappedTypes.ToDictionary(type => type,
                TemplateIdUtility.GetTypeIdentifier);

            try
            {
                return ConfigSerialization.Serialize(identifiers, templates, "the config set's types");
            }
            catch (Exception exception) when (exception is JsonException or ConfigException)
            {
                Error($"Baking failed: {exception.Message}");
                return null;
            }
        }

        private void MarkModified(Template template)
        {
            if (!EditorUtility.IsPersistent(template)) return;

            EditorUtility.SetDirty(template);
            if (!_modified.Contains(template)) _modified.Add(template);
        }

        private void Error(string message, Object context = null, int line = 0, Action fix = null)
        {
            _issues.Add(new ConfigIssue(ConfigIssueSeverity.Error, message, context, line, fix));
        }

        private void Warning(string message, Object context = null, int line = 0)
        {
            _issues.Add(new ConfigIssue(ConfigIssueSeverity.Warning, message, context, line));
        }

        private static List<Type> WithBaseTypes(List<Type> types)
        {
            List<Type> result = new();
            foreach (Type type in types)
            {
                for (Type baseType = type; baseType != typeof(Template); baseType = baseType.BaseType)
                {
                    if (!result.Contains(baseType)) result.Add(baseType);
                }
            }

            return result;
        }

        /// <summary>The template and asset references of a template, with their JSON path.</summary>
        private static List<(object Reference, string Path)> CollectReferences(Template template)
        {
            List<(object, string)> references = new();
            JsonSerializerSettings settings = ConfigSerialization.CreateSerializerSettings();
            settings.Converters.Insert(0, new ReferenceCollector(references));
            JToken.FromObject(template, JsonSerializer.CreateDefault(settings));
            return references;
        }

        private sealed class ReferenceCollector : JsonConverter
        {
            private readonly List<(object, string)> _references;

            public ReferenceCollector(List<(object, string)> references)
            {
                _references = references;
            }

            public override bool CanRead => false;

            public override bool CanConvert(Type objectType)
            {
                return typeof(ITemplateReference).IsAssignableFrom(objectType) ||
                       typeof(ITemplateAssetReference).IsAssignableFrom(objectType);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                _references.Add((value, writer.Path));
                writer.WriteNull();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class Entry
        {
            public Entry(Template template, Object context, int line, string source)
            {
                Template = template;
                Context = context;
                Line = line;
                Source = source;
            }

            public Template Template { get; }
            public Object Context { get; }
            public int Line { get; }
            public string Source { get; }
        }
    }
}
