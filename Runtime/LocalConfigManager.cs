using System;
using System.Collections;
using System.Collections.Generic;
using ToG.Scopes;
using UnityEngine;

namespace ToG.Config
{
    [CreateAssetMenu(fileName = "ConfigManager_Local", menuName = "Manager/Config Manager/Local")]
    public class LocalConfigManager : ConfigManager
    {
        [SerializeField] private ConfigStructure _container;

        private Dictionary<Type, IList> _templateCollections;
        private Dictionary<string, ITemplate> _templateLookup;

        public override IEnumerator InitializeAsync(IProgress<ScopeProgress> progress)
        {
            yield return InitializeLocal(progress);
            yield return base.InitializeAsync(progress);
        }

        private IEnumerator InitializeLocal(IProgress<ScopeProgress> progress)
        {
            _templateCollections = new();
            _templateLookup = new();

            progress.Report(new ScopeProgress(0f, "Collect templates"));
            for (int i = 0; i < _container.Groups.Count; i++)
            {
                var group = _container.Groups[i];
                Type type = Type.GetType(group.TypeString);
                Array array = Array.CreateInstance(type, group.MatchingTemplates.Count);
                _templateCollections.Add(type, array);

                progress.Report(new ScopeProgress(
                    (float)i / _container.Groups.Count * 0.9f,
                    $"Collect templates\n{type.FullName}"));
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