using Kott.Configs.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Kott.Configs
{
    public static class ConfigStructureUtility
    {
        private class GroupData
        {
            public Type Type { get; }
            public HashSet<Type> MatchingTypes { get; } = new();
            public List<Template> Templates { get; } = new();

            public GroupData(Type type)
            {
                Type = type;
            }
        }

        private class GroupComparer : IComparer<ConfigStructure.Group>
        {
            public int Compare(ConfigStructure.Group x, ConfigStructure.Group y)
            {
                for (int i = 0; i < x.BaseTypeStrings.Count && i < y.BaseTypeStrings.Count; i++)
                {
                    string xBaseTypeString = x.BaseTypeStrings[x.BaseTypeStrings.Count - 1 - i];
                    string yBaseTypeString = y.BaseTypeStrings[y.BaseTypeStrings.Count - 1 - i];

                    int baseTypeComparison = xBaseTypeString.CompareTo(yBaseTypeString);
                    if (baseTypeComparison != 0) return baseTypeComparison;
                }

                return x.BaseTypeStrings.Count.CompareTo(y.BaseTypeStrings.Count);
            }
        }

        private class TemplateReferenceCollection
        {
            public HashSet<ITemplateReference> TemplateReferencesDirect { get; } = new();
            public HashSet<ITemplateAssetReference> TemplateAssetReferencesDirect { get; } = new();

            public HashSet<ITemplateReference> TemplateReferencesCombined { get; } = new();
            public HashSet<ITemplateAssetReference> TemplateAssetReferencesCombined { get; } = new();

            public bool IsCombined { get; set; }
        }

        public static void UpdateConfigStructure()
        {
            Dictionary<Type, GroupData> groups = new();
            Dictionary<Type, ConfigStructure.Group> groupStructures = new();
            Dictionary<Template, TemplateReferenceCollection> templateReferences = new();

            // collect base data for each template
            // - create group data for each concrete type that shows up
            //   containing templates and matching types
            // - create reference data for each template containing all
            //   direct template and asset references
            foreach (Template template in ConfigEditorUtility.GetTemplatesAll())
            {
                Type type = template.GetType();
                if (!groups.TryGetValue(type, out GroupData groupData))
                {
                    groupData = new GroupData(type);
                    groups.Add(type, groupData);

                    CollectMatchingTypes(type, groupData.MatchingTypes);
                }

                groupData.Templates.Add(template);

                TemplateReferenceCollection referenceData = new();
                templateReferences.Add(template, referenceData);

                TemplateReferenceUtility.GetTemplateRefs(template,
                    referenceData.TemplateReferencesDirect,
                    referenceData.TemplateAssetReferencesDirect);
                referenceData.TemplateReferencesCombined.UnionWith(referenceData.TemplateReferencesDirect);
                referenceData.TemplateAssetReferencesCombined.UnionWith(referenceData.TemplateAssetReferencesDirect);

                // update asset references
                foreach (ITemplateAssetReference assetReference in referenceData.TemplateAssetReferencesDirect)
                {
                    UpdateAssetReference(template, assetReference);
                }
            }

            // combine each templates direct references with all references
            // of each directly referenced template
            foreach (Template template in ConfigEditorUtility.GetTemplatesAll())
            {
                CombineReferences(template, templateReferences);

                TemplateReferenceCollection referenceData = templateReferences[template];
                template.AssetAddressesCombined = referenceData.TemplateAssetReferencesCombined
                    .Where(assetRef => assetRef.Asset)
                    .Select(assetRef => assetRef.Address)
                    .ToArray();

                EditorUtility.SetDirty(template);

                // basic reference loop detection
                if (referenceData.TemplateReferencesCombined.Any(t => t.Template == template))
                {
                    Debug.Log($"[Config] Detected template reference loop: {template.name}");
                }
            }

            // create the combined group structures
            // - collect templates for each concrete and inherited type
            // - setup template labels based on matching types
            foreach (GroupData groupData in groups.Values)
            {
                string[] labels = groupData.MatchingTypes
                    .Select(t => ConfigEditorUtility.TypeToLabel(t))
                    .ToArray();

                TemplateGroupAttribute groupAttribute = groupData.Type.GetCustomAttribute<TemplateGroupAttribute>(true);
                string groupID = groupAttribute?.GroupID ?? groupData.Type.Name;

                foreach (Template template in groupData.Templates)
                {
                    AssetDatabase.SetLabels(template, labels);
                    if (AssetDatabase.IsSubAsset(template))
                    {
                        string subPath = AssetDatabase.GetAssetPath(template);
                        string name = Path.GetFileNameWithoutExtension(subPath);
                        template.ID = $"{groupID}+{name}+{template.name}";
                    }
                    else
                    {
                        template.ID = $"{groupID}+{template.name}";
                    }

                    EditorUtility.SetDirty(template);
                }

                foreach (Type matchingType in groupData.MatchingTypes)
                {
                    if (!groupStructures.TryGetValue(matchingType, out ConfigStructure.Group groupStructure))
                    {
                        groupStructure = new();
                        groupStructure.TypeString = matchingType.AssemblyQualifiedName;
                        groupStructures.Add(matchingType, groupStructure);

                        if (!matchingType.IsInterface)
                        {
                            Type type = matchingType;
                            while (type != null && type != typeof(Template))
                            {
                                groupStructure.BaseTypeStrings.Add(type.AssemblyQualifiedName);
                                type = type.BaseType;
                            }
                            groupStructure.BaseTypeStrings.Add(typeof(Template).AssemblyQualifiedName);
                        }
                    }

                    groupStructure.MatchingTemplates.AddRange(groupData.Templates);
                }

                groupStructures[groupData.Type].Templates.AddRange(groupData.Templates);
            }

            foreach ((Type groupType, ConfigStructure.Group groupStructure) in groupStructures)
            {
                TemplateGroupAttribute groupAttribute = groupType.GetCustomAttribute<TemplateGroupAttribute>(true);
                groupStructure.ID = groupAttribute?.GroupID ?? groupType.Name;
            }

            ConfigStructure configStructure = ConfigEditorUtility.GetConfigDataCache<ConfigStructure>();

            GroupComparer groupComparer = new GroupComparer();
            configStructure.Groups = groupStructures.Values
                .OrderBy(group => group, groupComparer)
                .ToList();

            EditorUtility.SetDirty(configStructure);
        }

        private static void CollectMatchingTypes(Type type, HashSet<Type> matchingTypes)
        {
            matchingTypes.Add(type);

            if (!type.IsInterface && type != typeof(Template))
            {
                Type baseType = type.BaseType;
                CollectMatchingTypes(baseType, matchingTypes);

                // TODO evaluate if template interfaces are needed
                // Type[] interfaceTypes = type.GetInterfaces();
                // for (int i = 0; i < interfaceTypes.Length; i++)
                //     CollectMatchingTypes(interfaceTypes[i], matchingTypes);
            }
        }

        private static void UpdateAssetReference(Template template, ITemplateAssetReference assetReference)
        {
            if (assetReference.Asset != null &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assetReference.Asset, out string guid, out long localId))
            {
                AddressableAssetEntry assetEntry;
                if ((assetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid)) == null)
                {
                    Debug.Log($"[Config] Creating asset entry in default group: guid={guid}");
                    assetEntry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(
                        guid, AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);
                }

                string key = assetEntry.address;
                if (!AssetDatabase.IsMainAsset(assetReference.Asset))
                {
                    key += $"[{assetReference.Asset.name}]";
                }

                if (assetReference.Address != key)
                {
                    Debug.Log($"[Config] Setting address of asset ref: {assetReference.Address} -> {key}");
                    assetReference.Address = key;
                    EditorUtility.SetDirty(template);
                }
            }
        }

        private static void CombineReferences(Template template,
            Dictionary<Template, TemplateReferenceCollection> data)
        {
            TemplateReferenceCollection referenceCollection = data[template];
            if (referenceCollection.IsCombined) return;
            referenceCollection.IsCombined = true;

            foreach (ITemplateReference reference in referenceCollection.TemplateReferencesDirect)
            {
                if (!reference.Template) continue;
                CombineReferences(reference.Template, data);
                TemplateReferenceCollection subData = data[reference.Template];
                referenceCollection.TemplateReferencesCombined.UnionWith(subData.TemplateReferencesCombined);
                referenceCollection.TemplateAssetReferencesCombined.UnionWith(subData.TemplateAssetReferencesCombined);
            }
        }
    }
}