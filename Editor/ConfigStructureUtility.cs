using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ToG.Config.Attributes;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ToG.Config
{
    public static class ConfigStructureUtility
    {
        private class GroupData
        {
            public Type Type;
            public HashSet<Type> MatchingTypes = new();
            public List<Template> Templates = new();

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

        private class TemplateReferenceData
        {
            public HashSet<ITemplateRef> TemplateRefsDirect = new();
            public HashSet<ITemplateAssetRef> TemplateAssetRefsDirect = new();

            public HashSet<ITemplateRef> TemplateRefsCombined = new();
            public HashSet<ITemplateAssetRef> TemplateAssetRefsCombined = new();

            public bool IsCombined;
        }

        public static void UpdateConfigStructure()
        {
            Dictionary<Type, GroupData> groups = new();
            Dictionary<Type, ConfigStructure.Group> groupStructures = new();
            Dictionary<Template, TemplateReferenceData> templateReferences = new();

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

                TemplateReferenceData referenceData = new();
                templateReferences.Add(template, referenceData);

                TemplateReferenceUtility.GetTemplateRefs(template,
                    referenceData.TemplateRefsDirect,
                    referenceData.TemplateAssetRefsDirect);
                referenceData.TemplateRefsCombined.UnionWith(referenceData.TemplateRefsDirect);
                referenceData.TemplateAssetRefsCombined.UnionWith(referenceData.TemplateAssetRefsDirect);

                // update asset references
                foreach (ITemplateAssetRef assetRef in referenceData.TemplateAssetRefsDirect)
                {
                    UpdateAssetReference(template, assetRef);
                }
            }

            // combine each templates direct references with all references
            // of each directly referenced template
            foreach (Template template in ConfigEditorUtility.GetTemplatesAll())
            {
                CombineReferences(template, templateReferences);

                TemplateReferenceData referenceData = templateReferences[template];
                template.AssetAddressesCombined = referenceData.TemplateAssetRefsCombined
                    .Where(assetRef => assetRef.Asset)
                    .Select(assetRef => assetRef.Address)
                    .ToArray();

                EditorUtility.SetDirty(template);

                // basic reference loop detection
                if (referenceData.TemplateRefsCombined.Any(t => t.Asset == template))
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

                TemplateGroupAttribute groupAttribute = groupData.Type.GetCustomAttribute<TemplateGroupAttribute>();
                string groupID = groupAttribute != null ? groupAttribute.GroupID : groupData.Type.Name;

                foreach (Template template in groupData.Templates)
                {
                    AssetDatabase.SetLabels(template, labels);
                    template.ID = $"{groupID}+{template.name}";
                    EditorUtility.SetDirty(template);
                }

                foreach (Type matchingType in groupData.MatchingTypes)
                {
                    if (!groupStructures.TryGetValue(matchingType, out var groupStructure))
                    {
                        groupStructure = new ConfigStructure.Group();
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

                Type[] interfaceTypes = type.GetInterfaces();
                for (int i = 0; i < interfaceTypes.Length; i++)
                    CollectMatchingTypes(interfaceTypes[i], matchingTypes);
            }
        }

        private static void UpdateAssetReference(Template template, ITemplateAssetRef assetRef)
        {
            if (assetRef.Asset != null &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assetRef.Asset, out string guid, out long localId))
            {
                AddressableAssetEntry assetEntry;
                if ((assetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid)) == null)
                {
                    Debug.Log($"[Config] Creating asset entry in default group: guid={guid}");
                    assetEntry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(
                        guid, AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);
                }

                string key = assetEntry.address;
                if (!AssetDatabase.IsMainAsset(assetRef.Asset))
                {
                    key += $"[{assetRef.Asset.name}]";
                }

                if (assetRef.Address != key)
                {
                    Debug.Log($"[Config] Setting address of asset ref: {assetRef.Address} -> {key}");
                    assetRef.Address = key;
                    EditorUtility.SetDirty(template);
                }
            }
        }

        private static void CombineReferences(Template template,
            Dictionary<Template, TemplateReferenceData> data)
        {
            TemplateReferenceData referenceData = data[template];
            if (referenceData.IsCombined) return;
            referenceData.IsCombined = true;

            foreach (var item in referenceData.TemplateRefsDirect)
            {
                if (!item.Asset) continue;
                CombineReferences(item.Asset, data);
                TemplateReferenceData subData = data[item.Asset];
                referenceData.TemplateRefsCombined.UnionWith(subData.TemplateRefsCombined);
                referenceData.TemplateAssetRefsCombined.UnionWith(subData.TemplateAssetRefsCombined);
            }
        }
    }
}