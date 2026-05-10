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
        [SerializeField] private ConfigStructure _container;

        private Dictionary<Type, IList> _templateCollections;
        private Dictionary<string, Template> _templateLookup;

        public IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            _templateCollections = new Dictionary<Type, IList>();
            _templateLookup = new Dictionary<string, Template>();

            progress.Report(new ProgressReport(0f, "Collect templates"));
            for (int i = 0; i < _container.Groups.Count; i++)
            {
                ConfigStructure.Group group = _container.Groups[i];
                Type type = Type.GetType(group.TypeString);
                Array array = Array.CreateInstance(type, group.MatchingTemplates.Count);
                _templateCollections.Add(type, array);

                progress.Report(new ProgressReport(
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