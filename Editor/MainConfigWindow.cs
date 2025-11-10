using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kott.Configs.Attributes;
using Kott.Configs.Converters;
using Unity.RemoteConfig.Editor;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    public class MainConfigWindow : EditorWindow
    {
        private const string ID_PATTERN = "^(?:[a-z]+)(?:_[a-z]+)*(?:_[0-9]+)?$";

        [MenuItem("Tools/Config Tool")]
        private static void Open() => GetWindow<MainConfigWindow>("Config Tool", true).Show();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull Configs")) PullConfigs();
            if (GUILayout.Button("Push Configs")) PushConfigs();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Table")) OpenTable();
            if (GUILayout.Button("Open Asset Checker")) OpenAssetChecker();
        }

        private void PullConfigs()
        {
            string projectId = CloudProjectSettings.projectId;

            RemoteConfigWebApiClient.fetchEnvironmentsFinished += FetchEnvironmentsFinished;
            RemoteConfigWebApiClient.FetchEnvironments(projectId);

            void FetchEnvironmentsFinished(JArray environments)
            {
                RemoteConfigWebApiClient.fetchEnvironmentsFinished -= FetchEnvironmentsFinished;
                Debug.Log($"FetchEnvironmentsFinished: {environments}");

                JToken targetEnvironment = environments.FirstOrDefault(env => env["name"].Value<string>() == "development");
                if (targetEnvironment != null)
                {
                    string environmentId = targetEnvironment["id"].Value<string>();

                    RemoteConfigWebApiClient.fetchConfigsFinished += FetchConfigsFinished;
                    RemoteConfigWebApiClient.FetchConfigs(projectId, environmentId);
                }
            }

            void FetchConfigsFinished(JObject configs)
            {
                RemoteConfigWebApiClient.fetchConfigsFinished -= FetchConfigsFinished;
                Debug.Log($"FetchConfigsFinished: {configs}");

                JObject entries = new JObject();

                foreach (JToken token in configs["value"])
                {
                    if (token is JObject entry &&
                        entry.TryGetValue("key", out JToken keyToken) &&
                        entry.TryGetValue("value", out JToken valueToken))
                    {
                        entries.Add(keyToken.Value<string>(), valueToken);
                    }
                }

                RCJsonAPI.WriteToConfigAsset("json_config", entries);
            }
        }

        private class GroupData
        {
            public Type Type;
            public HashSet<Type> MatchingTypes = new();
            public List<Template> Templates = new();
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

        private void PushConfigs()
        {
            Dictionary<Type, GroupData> groups = new();
            Dictionary<Type, ConfigStructure.Group> groupStructures = new();
            Dictionary<Template, HashSet<Template>> templateRefs = new();

            foreach (Template template in ConfigEditorUtility.GetTemplatesAll())
            {
                Type type = template.GetType();
                if (!groups.TryGetValue(type, out GroupData groupData))
                {
                    groupData = new GroupData();
                    groupData.Type = type;
                    groups.Add(type, groupData);

                    CollectMatchingTypes(type, groupData.MatchingTypes);
                }

                groupData.Templates.Add(template);

                templateRefs.Add(template, GetTemplateRefs(template));
            }

            _loopChecked.Clear();
            _loopHashCodes.Clear();
            foreach (Template template in templateRefs.Keys)
            {
                CheckForLoops(template, templateRefs);
            }

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
                    template.ID = $"{groupID}:{template.name}";
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

            // TODO make remote config push work again

            // JObject entries = new JObject();
            // 
            // JsonSerializer serializer = JsonSerializer.CreateDefault();
            // serializer.Converters.Add(new ConfigItemRefConverter());
            // serializer.Converters.Add(new Vector2Converter());
            // serializer.Converters.Add(new Vector2IntConverter());
            // serializer.Converters.Add(new Vector3Converter());
            // serializer.Converters.Add(new Vector3IntConverter());
            // serializer.Converters.Add(new Vector4Converter());
            // serializer.Converters.Add(new QuaternionConverter());
            // serializer.Converters.Add(new ColorConverter());
            // serializer.Converters.Add(new RectConverter());
            // serializer.Converters.Add(new RectIntConverter());
            // serializer.Converters.Add(new BoundsConverter());
            // serializer.Converters.Add(new BoundsIntConverter());
            // 
            // configStructure.Groups.Clear();
            // 
            // foreach (TemplateTypeData typeData in TemplateTypeData.GetAll())
            // {
            //     var group = new TemplateContainer.Group();
            //     configStructure.Groups.Add(group);
            // 
            //     group.Description.GroupID = typeData.GroupID;
            //     group.Description.TemplateType = typeData.Type;
            //     group.Description.TemplateTypeString = typeData.Type.AssemblyQualifiedName;
            //     foreach (var parentTypeData in typeData.Inherited)
            //         group.Description.ParentGroupIDs.Add(parentTypeData.GroupID);
            // 
            //     foreach (TemplateWrapper wrapper in TemplateWrapperUtility.GetWithType(typeData))
            //     {
            //         if (wrapper.Item == null)
            //             continue;
            // 
            //         group.Wrappers.Add(wrapper);
            //     }
            // }
            // 
            // entries.Add("config_description", JArray.FromObject(configStructure.Groups.Select(g => g.Description), serializer));
            // 
            // foreach (var group in configStructure.Groups)
            // {
            //     JArray items = new JArray();
            // 
            //     foreach (TemplateWrapper wrapper in group.Wrappers)
            //     {
            //         items.Add(JObject.FromObject(wrapper.Item, serializer));
            //     }
            // 
            //     entries.Add(group.Description.GroupID, items);
            // }
            // 
            // EditorUtility.SetDirty(configStructure);
            // RCJsonAPI.WriteToConfigAsset("json_config", entries);
        }

        private void CollectMatchingTypes(Type type, HashSet<Type> matchingTypes)
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

        private class TemplateRefConverter : JsonConverter<ITemplateRef>
        {
            public HashSet<Template> TemplatesRefs = new();

            public override ITemplateRef ReadJson(JsonReader reader, Type objectType, ITemplateRef existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, ITemplateRef value, JsonSerializer serializer)
            {
                string sValue = value.Asset != null ? sValue = value.Asset.ID : "";
                serializer.Serialize(writer, sValue, typeof(string));

                if (value.Asset != null)
                    TemplatesRefs.Add(value.Asset);
            }
        }

        private HashSet<Template> GetTemplateRefs(Template template)
        {
            TemplateRefConverter templateRefConverter = new TemplateRefConverter();

            JsonSerializer serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(templateRefConverter);
            serializer.Converters.Add(new Vector2Converter());
            serializer.Converters.Add(new Vector2IntConverter());
            serializer.Converters.Add(new Vector3Converter());
            serializer.Converters.Add(new Vector3IntConverter());
            serializer.Converters.Add(new Vector4Converter());
            serializer.Converters.Add(new QuaternionConverter());
            serializer.Converters.Add(new ColorConverter());
            serializer.Converters.Add(new RectConverter());
            serializer.Converters.Add(new RectIntConverter());
            serializer.Converters.Add(new BoundsConverter());
            serializer.Converters.Add(new BoundsIntConverter());

            JObject jObject = JObject.FromObject(template, serializer);
            return templateRefConverter.TemplatesRefs;
        }

        private HashSet<Template> _loopChecked = new();
        private HashSet<int> _loopHashCodes = new();
        private HashSet<Template> _loopCheckVisited = new();
        private Stack<string> _loopCheckPath = new();

        private void CheckForLoops(Template template,
            Dictionary<Template, HashSet<Template>> templateRefs)
        {
            if (!_loopChecked.Add(template))
                return;

            _loopCheckVisited.Clear();
            _loopCheckPath.Clear();
            CheckForLoops(template, template, templateRefs, _loopCheckVisited, _loopCheckPath);
        }

        private void CheckForLoops(Template templateRoot, Template template,
            Dictionary<Template, HashSet<Template>> templateRefs,
            HashSet<Template> visited, Stack<string> path)
        {
            if (!visited.Add(template))
            {
                // loop detected
                if (templateRoot == template)
                {
                    int loopHashCode = visited.Select(t => t.GetHashCode())
                        .OrderBy(h => h)
                        .Aggregate(0, (c, h) => HashCode.Combine(c, h));
                    if (_loopHashCodes.Add(loopHashCode))
                        Debug.Log($"Detected template ref loop:\n{string.Join('\n', path)}");
                }
                return;
            }

            path.Push(template.ID);

            foreach (Template templateRef in templateRefs[template])
            {
                CheckForLoops(templateRoot, templateRef, templateRefs, visited, path);
            }

            visited.Remove(template);
            path.Pop();
        }

        private void OpenTable()
        {
            TableWindow tableWindow = CreateWindow<TableWindow>();
            tableWindow.UpdateValues("", new ItemFilter());
            tableWindow.Show();
        }

        private void OpenAssetChecker()
        {
            AssetCheckerWindow tableWindow = GetWindow<AssetCheckerWindow>();
            tableWindow.Show();
        }
    }
}
