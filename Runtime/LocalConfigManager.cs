using Kott.Scopes;
using Kott.Utilities.Progress;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs
{
    public class LocalConfigManager : Manager, IConfigManager
    {
        [SerializeField] private ConfigStructure _container;

        private Dictionary<Type, IList> _templateCollections;
        private Dictionary<string, ITemplate> _templateLookup;

        public override IEnumerator InitializeAsync(IProgress<ProgressReport> progress)
        {
            yield return base.InitializeAsync(progress);
            yield return InitializeLocal(progress);
        }

        private IEnumerator InitializeLocal(IProgress<ProgressReport> progress)
        {
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