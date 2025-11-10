using Kott.Utilities.Progress;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs
{
    [CreateAssetMenu(fileName = "ConfigManager_Local", menuName = "Manager/Config Manager (Local)", order = 9002)]
    public class LocalConfigManager : ConfigManager
    {
        [SerializeField] private ConfigStructure _container;

        private Dictionary<Type, IList> _templateCollections;
        private Dictionary<string, ITemplate> _templateLookup;

        public override IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            yield return base.Initialize(progress);

            _templateCollections = new();
            _templateLookup = new();

            progress.Report(new(0f, "Collect templates"));

            for (int i = 0; i < _container.Groups.Count; i++)
            {
                var group = _container.Groups[i];
                Type type = Type.GetType(group.TypeString);
                Array array = Array.CreateInstance(type, group.MatchingTemplates.Count);
                _templateCollections.Add(type, array);

                progress.Report(new(
                    (float)i / _container.Groups.Count * 0.9f,
                    $"Collect templates - {type.FullName}"));
                yield return null;

                for (int j = 0; j < group.MatchingTemplates.Count; j++)
                {
                    Template template = group.MatchingTemplates[j];
                    array.SetValue(template, j);
                }

                for (int j = 0; j < group.Templates.Count; j++)
                {
                    Template template = group.Templates[j];
                    _templateLookup[template.ID] = template;
                }
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