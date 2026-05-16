using Kott.Scopes;
using Kott.Utilities.Progress;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kott.Utilities.JsonConverters;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    public class RemoteConfigManager : Manager, IConfigManager
    {
        private struct UserAttributes
        {
            // Optionally declare variables for any custom user attributes:
            public bool expansionFlag;
        }

        private struct AppAttributes
        {
            // Optionally declare variables for any custom app attributes:
            public int level;
            public int score;
            public string appVersion;
        }

        private struct FilterAttributes
        {
            // Optionally declare variables for attributes to filter on any of following parameters:
            public string[] key;
            public string[] type;
            public string[] schemaId;
        }

        private Dictionary<Type, IList> _templateCollections;
        private Dictionary<string, Template> _templateLookup;

        public override IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            yield return new WaitForTaskWithProgress(InitializeRemote(), progress);
        }

        private async Task InitializeRemote()
        {
            RemoteConfigService.Instance.FetchCompleted += OnConfigFetchCompleted;
            RuntimeConfig config = await RemoteConfigService.Instance.FetchConfigsAsync(
                new UserAttributes(), new AppAttributes(), new FilterAttributes());

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
            
            HashSet<string> groupIds = new();
            
            _templateCollections = new();
            _templateLookup = new();
            
            JArray jGroups = GetConfigData(config, "template_groups");
            foreach (JObject jGroup in jGroups)
            {
                string groupId = jGroup.Value<string>("id");
                string groupTypeString = jGroup.Value<string>("type");
                JArray jTemplatesInGroup =  jGroup["templates"] as JArray;
                
                Type groupType = Type.GetType(groupTypeString);
                Array array = jTemplatesInGroup.Values<string>().ToArray();
                _templateCollections.Add(groupType, array);
                
                if (!groupIds.Add(groupId)) continue;
                
                JArray jTemplates = GetConfigData(config, groupId);
                if (jTemplates == null || jTemplates.Count == 0) continue;

                foreach (JObject jTemplate in jTemplates)
                {
                    string templateId = jTemplate.Value<string>("ID");
                    string typeString = jTemplate.Value<string>("$type");
                    
                    Type type = Type.GetType(typeString);

                    Template template = ScriptableObject.CreateInstance(type) as Template;
                    serializer.Populate(jTemplate.CreateReader(), template);
                    _templateLookup.Add(templateId, template);
                }
            }

            foreach (Type type in _templateCollections.Keys.ToList())
            {
                string[] templateIds = (string[])_templateCollections[type];
                Array templates = Array.CreateInstance(type, templateIds.Length);
                _templateCollections[type] = templates;
                for (int i = 0; i < templateIds.Length; i++)
                {
                    Template template = _templateLookup[templateIds[i]];
                    templates.SetValue(template, i);
                }
            }
            
            foreach ((string id, List<ITemplateReference> references) in templateReferenceConverter.References)
            {
                Template template = _templateLookup.GetValueOrDefault(id);
                foreach (ITemplateReference reference in references)
                {
                    reference.Template = template;
                }
            }
        }

        private JArray GetConfigData(RuntimeConfig config, string key)
        {
            string configDescriptionJson = config.GetJson(key, "[]");
            using (TextReader textReader = new StringReader(configDescriptionJson))
            using (JsonReader jsonReader = new JsonTextReader(textReader))
            {
                var array = JToken.ReadFrom(jsonReader) as JArray;
                Debug.Log(array.ToString(Formatting.Indented));
                return array;
            }
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