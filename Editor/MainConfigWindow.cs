using Newtonsoft.Json.Linq;
using System.Linq;
using Unity.RemoteConfig.Editor;
using UnityEditor;
using UnityEngine;

namespace ToG.Config
{
    public class MainConfigWindow : EditorWindow
    {
        [MenuItem("Tools/Configs/Open Window", priority = 0)]
        private static void Open() => GetWindow<MainConfigWindow>("Config Tool", true).Show();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull Configs")) PullConfigs();
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

            RemoteConfigWebApiClient.fetchEnvironmentsFinished += FetchEnvironmentsFinished;
            RemoteConfigWebApiClient.FetchEnvironments(projectId);

            void FetchEnvironmentsFinished(JArray environments)
            {
                RemoteConfigWebApiClient.fetchEnvironmentsFinished -= FetchEnvironmentsFinished;
                Debug.Log($"[Config] FetchEnvironmentsFinished: {environments}");

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

                RCJsonAPI.WriteToConfigAsset("json_config", entries);
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
    }
}
