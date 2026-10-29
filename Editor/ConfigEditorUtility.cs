using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kott.Configs.Attributes;
using UnityEditor;

namespace Kott.Configs
{
    internal static class ConfigEditorUtility
    {
        private static readonly Dictionary<Type, string> TypeDisplayNames = new();

        /// <summary>
        ///     All template types with their inheritance depth below <see cref="Template" />, base types before their
        ///     subtypes, siblings by name.
        /// </summary>
        /// <param name="includedTypes">If set, only these types and their base types.</param>
        public static List<(Type Type, int Depth)> GetTemplateTypeTree(ICollection<Type> includedTypes = null)
        {
            List<(Type, int)> tree = new();
            ILookup<Type, Type> subtypes = TypeCache.GetTypesDerivedFrom<Template>()
                .Where(type => !type.ContainsGenericParameters)
                .Where(type => includedTypes == null || includedTypes.Any(type.IsAssignableFrom))
                .ToLookup(type => type.BaseType);
            AddSubtypes(typeof(Template), 0, subtypes, tree);
            return tree;
        }

        public static string GetTypeDisplayName(Type type)
        {
            if (!TypeDisplayNames.TryGetValue(type, out string displayName))
            {
                TemplateGroupAttribute groupAttribute = type.GetCustomAttribute<TemplateGroupAttribute>();
                displayName = groupAttribute?.DisplayName ?? type.Name;
                TypeDisplayNames.Add(type, displayName);
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
