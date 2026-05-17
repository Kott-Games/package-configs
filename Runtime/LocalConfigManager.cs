using System;
using System.Collections;
using System.Collections.Generic;
using Kott.Scopes;
using Kott.Utilities.Progress;
using UnityEngine;

namespace Kott.Configs
{
    public sealed class LocalConfigManager : Manager, IConfigManager
    {
        [SerializeField] private ConfigTypeMapping _configTypeMapping;
        [SerializeField] private RuntimeConfigData _configData;
        
        private readonly Dictionary<Type, IList> _templateCollections = new();
        private readonly Dictionary<string, Template> _templateLookup = new();

        public override IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            _templateCollections.Clear();
            _templateLookup.Clear();
            
            SubProgress subProgress = new(progress);

            subProgress.Start = new(0f, "Extract Configs");
            subProgress.Scale = 1f;
            
            yield return ExtractConfigData(subProgress, _configData);
        }

        private IEnumerator ExtractConfigData(IProgress<ProgressReport> progress, RuntimeConfigData configData)
        {
            Dictionary<Type, List<Template>> templateGroupCollections = new();
            
            foreach (Template template in configData.Templates)
            {
                _templateLookup.Add(template.ID, template);
                Type type = template.GetType();
                List<Template> groupTemplates = templateGroupCollections.GetValueOrDefault(type) ?? new List<Template>();
                templateGroupCollections[type] = groupTemplates;
                groupTemplates.Add(template);
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