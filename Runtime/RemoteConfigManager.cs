using Kott.Utilities.Progress;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace Kott.Configs
{
    [CreateAssetMenu(fileName = "ConfigManager_Remote", menuName = "Manager/Config Manager (Remote)", order = 9002)]
    public class RemoteConfigManager : ConfigManager
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

        private class ConfigItemRefConverter : JsonConverter<ITemplateRef>
        {
            public struct ItemRefInfo
            {
                public ITemplateRef ItemRef;
                public string GroupID;
                public string ID;
            }

            public List<ItemRefInfo> ItemRefInfos = new List<ItemRefInfo>();

            public override ITemplateRef ReadJson(JsonReader reader, Type objectType, ITemplateRef existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (existingValue == null)
                {
                    existingValue = (ITemplateRef)Activator.CreateInstance(objectType);
                }

                string info = serializer.Deserialize<string>(reader);
                if (!string.IsNullOrEmpty(info))
                {
                    string[] splitInfo = info.Split(':');
                    ItemRefInfos.Add(new ItemRefInfo()
                    {
                        ItemRef = existingValue,
                        GroupID = splitInfo[0],
                        ID = splitInfo[1],
                    });
                }

                return existingValue;
            }

            public override void WriteJson(JsonWriter writer, ITemplateRef value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        private Dictionary<Type, IList> _templateCollections;
        private Dictionary<string, object> _templateLookup;

        public override IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            yield return base.Initialize(progress);

            RemoteConfigService.Instance.FetchCompleted += OnConfigFetchCompleted;
            var fetchConfigsTask = RemoteConfigService.Instance.FetchConfigsAsync(
                new UserAttributes(), new AppAttributes(), new FilterAttributes());
            yield return new WaitUntil(() => fetchConfigsTask.IsCompleted);
            RuntimeConfig config = fetchConfigsTask.Result;

            ConfigItemRefConverter itemRefConverter = new ConfigItemRefConverter();

            JsonSerializer serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(itemRefConverter);

            string configDescriptionJson = config.GetJson("config_description", "{}");
            TemplateDescription[] descriptions = null;
            using (TextReader textReader = new StringReader(configDescriptionJson))
            using (JsonReader jsonReader = new JsonTextReader(textReader))
            {
                descriptions = serializer.Deserialize<TemplateDescription[]>(jsonReader);
            }

            _templateCollections = new();
            _templateLookup = new();

            Dictionary<string, Type> groupMapping = new();

            // collect templates
            for (int i = 0; i < descriptions.Length; i++)
            {
                TemplateDescription templateDescription = descriptions[i];

                groupMapping.Add(templateDescription.GroupID, templateDescription.TemplateType);

                string json = config.GetJson(templateDescription.GroupID, "[]");
                Type listType = typeof(List<>).MakeGenericType(templateDescription.TemplateType);

                using (TextReader textReader = new StringReader(json))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                {
                    IList items = (IList)serializer.Deserialize(jsonReader, listType);
                    _templateCollections.Add(templateDescription.TemplateType, items);

                    for (int j = 0; j < items.Count; j++)
                    {
                        Template template = (Template)items[j];
                        _templateLookup[template.ID] = template;
                    }

                    for (int j = 0; j < templateDescription.ParentGroupIDs.Count; j++)
                    {
                        string inherited = templateDescription.ParentGroupIDs[j];
                        Type type = groupMapping[inherited];
                        IList list = _templateCollections[type];

                        for (int k = 0; k < items.Count; k++)
                            list.Add(items[k]);
                    }
                }
            }

            // convert lists to arrays
            for (int i = 0; i < descriptions.Length; i++)
            {
                TemplateDescription templateDescription = descriptions[i];
                IList list = _templateCollections[templateDescription.TemplateType];
                Array array = Array.CreateInstance(templateDescription.TemplateType, list.Count);
                list.CopyTo(array, 0);
                _templateCollections[templateDescription.TemplateType] = array;
            }

            // resolve template references
            foreach (ConfigItemRefConverter.ItemRefInfo itemRefInfo in itemRefConverter.ItemRefInfos)
            {
                Type type = groupMapping[itemRefInfo.GroupID];
                Template item = _templateCollections[type]
                    .Cast<Template>()
                    .FirstOrDefault(i => i.ID == itemRefInfo.ID);

                itemRefInfo.ItemRef.Asset = item;
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

        public override IReadOnlyList<T> GetCollection<T>()
        {
            return (IReadOnlyList<T>)_templateCollections[typeof(T)];
        }

        public override T GetTemplate<T>(string key)
        {
            return (T)_templateLookup[key];
        }
    }
}