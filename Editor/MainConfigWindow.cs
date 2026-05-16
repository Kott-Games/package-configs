using System;
using Kott.Utilities.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Unity.RemoteConfig.Editor;
using Unity.Services.Core.Editor.Environments;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kott.Configs
{
    public class MainConfigWindow : EditorWindow
    {
        [MenuItem("Tools/Configs/Open Window", priority = 0)]
        private static void Open() => GetWindow<MainConfigWindow>("Config Tool", true).Show();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
            {
                // pulling in the editor is stupid if i think about it
                // we only ever change configs in the editor, so we only really need to push, not pull
                if (GUILayout.Button("Pull Configs")) PullConfigs();
            }
            if (GUILayout.Button("Push Configs")) PushConfigs();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Table")) OpenTable();
        }

        [MenuItem("Tools/Configs/Pull #F5", priority = 10)]
        private static void PullConfigs()
        {
            // TODO make remote config pull work again
            Debug.Log($"[Config] Pull Configs");

            string projectId = CloudProjectSettings.projectId;
            string environmentId = EnvironmentsApi.Instance.ActiveEnvironmentId.ToString();
            
            // RemoteConfigWebApiClient.fetchDefaultEnvironmentFinished += FetchDefaultEnvironmentFinished;
            // RemoteConfigWebApiClient.FetchDefaultEnvironment(projectId);
            
            RemoteConfigWebApiClient.fetchConfigsFinished += FetchConfigsFinished;
            RemoteConfigWebApiClient.FetchConfigs(projectId, environmentId);

            void FetchDefaultEnvironmentFinished(string environmentId)
            {
                RemoteConfigWebApiClient.fetchDefaultEnvironmentFinished -= FetchDefaultEnvironmentFinished;
                Debug.Log($"[Config] FetchDefaultEnvironmentFinished: {environmentId}");
                
                RemoteConfigWebApiClient.fetchConfigsFinished += FetchConfigsFinished;
                RemoteConfigWebApiClient.FetchConfigs(projectId, environmentId);
            }

            void FetchConfigsFinished(JObject configs)
            {
                RemoteConfigWebApiClient.fetchConfigsFinished -= FetchConfigsFinished;
                Debug.Log($"[Config] FetchConfigsFinished: {configs}");

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

                DeserializeConfigs(entries);
                // RCJsonAPI.WriteToConfigAsset("json_config", entries);
            }
        }
        
        [MenuItem("Tools/Configs/Push #F6", priority = 11)]
        private static void PushConfigs()
        {
            Debug.Log($"[Config] Push Configs");
            ConfigStructureUtility.UpdateConfigStructure();
            UpdateRemoteData();
        }

        [MenuItem("Tools/Configs/Open Table #F1", priority = 1)]
        private static void OpenTable()
        {
            TableWindow tableWindow = CreateWindow<TableWindow>();
            tableWindow.UpdateValues("", new ItemFilter());
            tableWindow.Show();
        }

        private static void UpdateRemoteData()
        {
            JObject entries = SerializeConfigs();
            RCJsonAPI.WriteToConfigAsset("json_config", entries);
        }

        private static JObject SerializeConfigs()
        {
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
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

            JObject entries = new JObject();
            JArray jGroups = new();
            entries.Add("template_groups", jGroups);
            Dictionary<string, JArray> templateArrays = new();
            
            ConfigStructure configStructure = ConfigEditorUtility.GetConfigDataCache<ConfigStructure>();
            foreach (ConfigStructure.Group group in configStructure.Groups)
            {
                JObject jGroup = new();
                jGroups.Add(jGroup);

                jGroup.Add("id", group.ID);
                jGroup.Add("type", group.TypeString);
                jGroup.Add("templates", JArray.FromObject(group.MatchingTemplates.Select(t => t.ID)));

                if (group.Templates.Count <= 0) continue;

                if (!templateArrays.TryGetValue(group.ID, out JArray jTemplates))
                {
                    jTemplates = new();
                    templateArrays.Add(group.ID, jTemplates);
                    entries.Add(group.ID, jTemplates);
                }

                foreach (Template template in group.Templates)
                {
                    JObject jTemplate = JObject.FromObject(template, serializer);
                    jTemplate.AddFirst(new JProperty("$type", group.TypeString));

                    // remove asset properties
                    jTemplate.Remove("name");
                    jTemplate.Remove("hideFlags");

                    jTemplates.Add(jTemplate);
                }
            }

            return entries;
        }

        private static void DeserializeConfigs(JObject entries)
        {
            TemplateReferenceConverter templateReferenceConverter = new TemplateReferenceConverter();
            TemplateAssetReferenceConverter templateAssetReferenceConverter = new TemplateAssetReferenceConverter();

            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
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
            
            HashSet<string> requiredFolders = new();
            HashSet<string> groupIds = new();
            Dictionary<string, (Template template, string assetPath)> templates = new();

            JArray jGroups = entries["template_groups"] as JArray;
            foreach (JObject jGroup in jGroups)
            {
                string groupId = jGroup.Value<string>("id");
                if (!groupIds.Add(groupId)) continue;
                
                JArray jTemplates = entries[groupId] as JArray;
                if (jTemplates == null || jTemplates.Count == 0) continue;

                Debug.Log($"[Config] Deserializing group {groupId}");

                string groupFolder = $"Assets/Configs/{groupId}";
                requiredFolders.Add(groupFolder);
                
                if (!AssetDatabase.IsValidFolder(groupFolder))
                {
                    AssetDatabase.CreateFolder("Assets/Configs", groupId);
                }

                foreach (JObject jTemplate in jTemplates)
                {
                    string templateId = jTemplate.Value<string>("ID");
                    string typeString = jTemplate.Value<string>("$type");
                    
                    // remove group id prefix
                    string shortId = templateId.Substring(groupId.Length + 1);
                    
                    Debug.Log($"[Config]   Deserializing template {shortId} with type {typeString}");

                    Type type = Type.GetType(typeString);

                    string assetPath = $"Assets/Configs/{groupId}/{shortId}.asset";
                    Template template = AssetDatabase.LoadAssetAtPath<Template>(assetPath);
                    if (!template || !template.GetType().IsEquivalentTo(type))
                    {
                        template = ScriptableObject.CreateInstance(type) as Template;
                    }
                    
                    serializer.Populate(jTemplate.CreateReader(), template);
                    templates.Add(templateId, (template, assetPath));
                }
            }
            
            foreach ((string id, List<ITemplateReference> references) in templateReferenceConverter.References)
            {
                Template template = templates.GetValueOrDefault(id).template;
                foreach (ITemplateReference reference in references)
                {
                    reference.Template = template;
                }
            }

            List<AddressableAssetEntry> addressableAssetEntries = new();
            AddressableAssetSettingsDefaultObject.Settings.GetAllAssets(addressableAssetEntries, true);
            Dictionary<string, Object> addressableAssetLookup = addressableAssetEntries
                .ToDictionary(e => e.address, e => e.MainAsset);

            foreach ((string id, List<ITemplateAssetReference> references) in templateAssetReferenceConverter.References)
            {
                Object asset = addressableAssetLookup.GetValueOrDefault(id);
                foreach (ITemplateAssetReference reference in references)
                {
                    reference.Asset = asset;
                    reference.Address = id;
                }
            }

            string[] subFolders = AssetDatabase.GetSubFolders("Assets/Configs");
            
            HashSet<string> existingPaths = new(AssetDatabase.FindAssets("t:Template", subFolders).Select(AssetDatabase.GUIDToAssetPath));
            HashSet<string> requiredPaths = new(templates.Select(p => p.Value.assetPath));
            
            HashSet<string> deletePaths = new(existingPaths);
            deletePaths.ExceptWith(requiredPaths);
            
            foreach (string path in deletePaths)
            {
                // Debug.Log($"Delete Asset {path}");
                AssetDatabase.DeleteAsset(path);
            }
            
            foreach (var pair in templates)
            {
                // Debug.Log($"Create Asset {pair.Value.assetPath}");
                AssetDatabase.CreateAsset(pair.Value.template, pair.Value.assetPath);
            }
            
            HashSet<string> deleteFolders = new(subFolders);
            deleteFolders.ExceptWith(requiredFolders);
            foreach (string path in deleteFolders)
            {
                // Debug.Log($"Delete Folder {path}");
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
