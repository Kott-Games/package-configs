using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kott.Scopes;
using Kott.Utilities.Progress;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace Kott.Configs
{
    public class ConfigManager : Manager, IConfigManager
    {
        private struct userAttributes {
            // Optionally declare variables for any custom user attributes:
            public bool expansionFlag;
        }

        private struct appAttributes {
            // Optionally declare variables for any custom app attributes:
            public int level;
            public int score;
            public string appVersion;
        }

        private struct filterAttributes {
            // Optionally declare variables for attributes to filter on any of following parameters:
            public string[] key;
            public string[] type;
            public string[] schemaId;
        }
        
        [SerializeField] private ConfigTypeMapping _configTypeMapping;

        [Header("Development")]
        [SerializeField] private bool _useLocalConfigData = false;
        [SerializeField] private RuntimeConfigData _configData;
        
        private readonly Dictionary<Type, IList> _templateCollections = new();
        private readonly Dictionary<string, Template> _templateLookup = new();

        public override IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            _templateCollections.Clear();
            _templateLookup.Clear();
            
            SubProgress subProgress = new(progress);
            
            if (_useLocalConfigData)
            {
                subProgress.Start = new(0f, "Extract Configs");
                subProgress.Scale = 1f;
            
                yield return ExtractConfigData(subProgress, _configData);
                
                yield break;
            }
            
            subProgress.Start = new(0f, "Fetch Remote Configs");
            subProgress.Scale = 0.5f;
            
            RuntimeConfigData configData = ScriptableObject.CreateInstance<RuntimeConfigData>();
            yield return FetchRemoteConfigs(subProgress, configData);

            subProgress.Start = new(0.5f, "Extract Configs");
            subProgress.Scale = 0.5f;
            
            yield return ExtractConfigData(subProgress, configData);
        }

        private IEnumerator FetchRemoteConfigs(IProgress<ProgressReport> progress, RuntimeConfigData configData)
        {
            SubProgress subProgress = new(progress);

            subProgress.Start = new(0f, "Fetch Remote Configs");
            subProgress.Scale = 0.5f;
            
            RemoteConfigService.Instance.FetchCompleted += OnConfigFetchCompleted;
            Task<RuntimeConfig> fetchConfigsAsync = RemoteConfigService.Instance
                .FetchConfigsAsync(new userAttributes(), new appAttributes(), new filterAttributes());
            yield return new WaitForTaskWithProgress(fetchConfigsAsync, subProgress);
            
            RuntimeConfig config = RemoteConfigService.Instance.appConfig;

            string jTemplatesString = config.GetJson("configTemplates", "[]");
            using (TextReader textReader = new StringReader(jTemplatesString))
            using (JsonReader jsonReader = new JsonTextReader(textReader))
            {
                subProgress.Start = new(0.5f, "Read Configs");
                subProgress.Scale = 0.5f;
                
                Task<JToken> readTokenAsync = JToken.ReadFromAsync(jsonReader);
                yield return new WaitForTaskWithProgress(readTokenAsync, subProgress);
                
                JArray jTemplates = readTokenAsync.Result as JArray;
                Debug.Log(jTemplates.ToString(Formatting.Indented));
                
                List<Template> templates = ConfigSerializationUtility.Deserialize(_configTypeMapping, jTemplates);
                configData.Templates.AddRange(templates);
            }
        }

        private IEnumerator ExtractConfigData(IProgress<ProgressReport> progress, RuntimeConfigData configData)
        {
            Dictionary<Type, List<Template>> templateGroupCollectionsBase = new();
            Dictionary<Type, List<Template>> templateGroupCollections = new();
            
            foreach (Template template in configData.Templates)
            {
                _templateLookup.Add(template.ID, template);
                Type type = template.GetType();
                List<Template> groupTemplates = templateGroupCollectionsBase.GetValueOrDefault(type) ?? new List<Template>();
                templateGroupCollectionsBase[type] = groupTemplates;
                groupTemplates.Add(template);
            }
            
            foreach (ConfigTypeMapping.Group typeGroup in _configTypeMapping.TypeGroups)
            {
                Type type = Type.GetType(typeGroup.TypeString);
                List<Template> groupTemplatesBase = templateGroupCollectionsBase.GetValueOrDefault(type);
                if (groupTemplatesBase == null) continue;
                foreach (string baseTypeString in typeGroup.BaseTypeStrings)
                {
                    Type baseType = Type.GetType(baseTypeString);
                    List<Template> groupTemplates = templateGroupCollections.GetValueOrDefault(baseType) ?? new List<Template>();
                    templateGroupCollections[baseType] = groupTemplates;
                    groupTemplates.AddRange(groupTemplatesBase);
                }
            }
            
            foreach (ConfigTypeMapping.Group typeGroup in _configTypeMapping.TypeGroups)
            {
                Type type = Type.GetType(typeGroup.TypeString);
                List<Template> groupTemplates = templateGroupCollections.GetValueOrDefault(type);
                Array groupTemplatesArray = Array.CreateInstance(type, groupTemplates?.Count ?? 0);
                _templateCollections[type] = groupTemplatesArray;
                if (groupTemplates == null) continue;
                for (var i = 0; i < groupTemplates.Count; i++)
                {
                    groupTemplatesArray.SetValue(groupTemplates[i], i);
                }
            }
            
            progress?.Report(new ProgressReport(1f, null));
            yield break;
        }

        private void OnConfigFetchCompleted(ConfigResponse response)
        {
            Debug.Log($"Config Fetch {response.status}.");

            switch (response.requestOrigin)
            {
                case ConfigOrigin.Default:
                    Debug.Log("No settings loaded this session and no local cache file exists; using default values.");
                    break;
                case ConfigOrigin.Cached:
                    Debug.Log("No settings loaded this session; using cached values from a previous session.");
                    break;
                case ConfigOrigin.Remote:
                    Debug.Log("New settings loaded this session; update values accordingly.");
                    break;
            }
        }

        public IReadOnlyList<T> GetCollection<T>()
            where T : Template
        {
            return (IReadOnlyList<T>)_templateCollections[typeof(T)];
        }

        public T GetTemplate<T>(string key)
            where T : Template
        {
            return (T)_templateLookup[key];
        }
    }
}