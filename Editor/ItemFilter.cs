using System;
using System.Collections.Generic;
using System.Linq;
using Kott.Utilities;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [Serializable]
    public class ItemFilter
    {
        [SerializeField] private List<string> _selectedTypeStrings = new List<string>();

        [SerializeField] private bool _configTypeSelectionExpanded;
        [SerializeField] private string _selectedConfigTypesHeader;

        public ItemFilter()
        {
            HandleChangedTypes();
        }

        public ItemFilter(ItemFilter other)
        {
            _selectedTypeStrings.AddRange(other._selectedTypeStrings);
            HandleChangedTypes();
        }


        public IEnumerable<Template> GetTemplates()
        {
            return _selectedTypeStrings
                .Select(ts => Type.GetType(ts))
                .SelectMany(t => ConfigEditorUtility.GetTemplatesWithType(t))
                .Distinct();
        }


        public bool DrawGUILayout()
        {
            bool typesChanged = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button(_selectedConfigTypesHeader))
            {
                _configTypeSelectionExpanded = !_configTypeSelectionExpanded;
            }

            if (_configTypeSelectionExpanded)
            {
                HashSet<string> nextTypeStrings = new HashSet<string>(_selectedTypeStrings);

                ConfigStructure configStructure = ConfigEditorUtility.GetConfigDataCache<ConfigStructure>();
                foreach (var group in configStructure.Groups)
                {
                    Type type = Type.GetType(group.TypeString);
                    if (type.IsInterface)
                        continue;

                    int depth = group.BaseTypeStrings.Count;
                    string displayName = ConfigEditorUtility.GetTypeDisplayName(type);

                    Rect position = EditorGUILayout.GetControlRect(false);
                    position.x += depth * 20.0f;
                    position.width -= depth * 20.0f;

                    bool isSelected = nextTypeStrings.Contains(group.TypeString);

                    Color backgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = isSelected ? Color.green : Color.red;

                    if (GUI.Button(position, displayName, EditorStyles.helpBox))
                    {
                        typesChanged = true;
                        bool changeDerived = Event.current.alt;
                        if (isSelected)
                        {
                            nextTypeStrings.Remove(group.TypeString);
                        }
                        else
                        {
                            nextTypeStrings.Add(group.TypeString);
                        }
                    }
                    GUI.backgroundColor = backgroundColor;
                }

                if (typesChanged)
                {
                    _selectedTypeStrings = nextTypeStrings.ToList();
                    HandleChangedTypes();
                }
            }

            EditorGUILayout.EndVertical();

            return typesChanged;
        }

        private void HandleChangedTypes()
        {
            _selectedConfigTypesHeader = _selectedTypeStrings
                .Select(ts => Type.GetType(ts))
                .Select(t => ConfigEditorUtility.GetTypeDisplayName(t))
                .JoinString(" | ");
        }
    }
}
