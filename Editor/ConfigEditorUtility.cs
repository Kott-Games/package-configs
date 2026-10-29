using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kott.Configs.Attributes;
using UnityEditor;

namespace Kott.Configs
{
    public class ConfigEditorUtility
    {
        private static Dictionary<Type, string> _typeDisplayNames = new();

        public static IEnumerable<Template> GetTemplatesWithType(Type type)
        {
            return AssetDatabase.FindAssets($"t:{type.FullName}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .SelectMany(assetPath => AssetDatabase.LoadAllAssetsAtPath(assetPath))
                .Select(asset => asset as Template)
                .Where(template => template);
        }

        /// <summary>
        ///     All template types with their inheritance depth below <see cref="Template" />, base types before their
        ///     subtypes, siblings by name.
        /// </summary>
        public static List<(Type Type, int Depth)> GetTemplateTypeTree()
        {
            List<(Type, int)> tree = new();
            ILookup<Type, Type> subtypes = TypeCache.GetTypesDerivedFrom<Template>()
                .Where(type => !type.ContainsGenericParameters)
                .ToLookup(type => type.BaseType);
            AddSubtypes(typeof(Template), 0, subtypes, tree);
            return tree;
        }

        public static string GetTypeDisplayName(Type type)
        {
            if (!_typeDisplayNames.TryGetValue(type, out string displayName))
            {
                TemplateGroupAttribute groupAttribute = type.GetCustomAttribute<TemplateGroupAttribute>();
                displayName = groupAttribute?.DisplayName ?? type.Name;
                _typeDisplayNames.Add(type, displayName);
            }

            return displayName;
        }

        private static void AddSubtypes(Type type, int depth, ILookup<Type, Type> subtypes, List<(Type, int)> tree)
        {
            foreach (Type subtype in subtypes[type].OrderBy(subtype => subtype.Name, StringComparer.Ordinal))
            {
                tree.Add((subtype, depth));
                AddSubtypes(subtype, depth + 1, subtypes, tree);
            }
        }
    }
}
