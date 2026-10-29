using System;
using System.Collections.Generic;
using System.Linq;
using Kott.Utilities;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>Template type selection of the template browser; a selected type includes its subtypes.</summary>
    [Serializable]
    internal class TemplateTypeFilter
    {
        [SerializeField]
        private List<string> _selectedTypeStrings = new();

        [SerializeField]
        private bool _expanded = true;

        private string _header;

        public TemplateTypeFilter()
        {
        }

        public TemplateTypeFilter(TemplateTypeFilter other)
        {
            _selectedTypeStrings.AddRange(other._selectedTypeStrings);
            _expanded = false;
        }

        /// <summary>The templates that are instances of a selected type.</summary>
        public IEnumerable<Template> Filter(IEnumerable<Template> templates)
        {
            List<Type> types = GetSelectedTypes().ToList();
            return templates.Where(template => types.Any(type => type.IsInstanceOfType(template)));
        }

        /// <summary>Draws the type tree, limited to <paramref name="availableTypes" /> and their base types.</summary>
        /// <returns>Whether the selection changed.</returns>
        public bool DrawGUILayout(ICollection<Type> availableTypes)
        {
            bool typesChanged = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _header ??= GetSelectedTypes().Select(ConfigEditorUtility.GetTypeDisplayName).JoinString(" | ");
            if (GUILayout.Button(_header.Length > 0 ? _header : "Select template types"))
            {
                _expanded = !_expanded;
            }

            if (_expanded)
            {
                HashSet<string> nextTypeStrings = new(_selectedTypeStrings);

                foreach ((Type type, int depth) in ConfigEditorUtility.GetTemplateTypeTree(availableTypes))
                {
                    string typeString = type.AssemblyQualifiedName;
                    string displayName = ConfigEditorUtility.GetTypeDisplayName(type);

                    Rect position = EditorGUILayout.GetControlRect(false);
                    position.x += depth * 20.0f;
                    position.width -= depth * 20.0f;

                    bool isSelected = nextTypeStrings.Contains(typeString);

                    Color backgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = isSelected ? Color.green : Color.red;

                    if (GUI.Button(position, displayName, EditorStyles.helpBox))
                    {
                        typesChanged = true;
                        if (isSelected)
                        {
                            nextTypeStrings.Remove(typeString);
                        }
                        else
                        {
                            nextTypeStrings.Add(typeString);
                        }
                    }

                    GUI.backgroundColor = backgroundColor;
                }

                if (typesChanged)
                {
                    _selectedTypeStrings = nextTypeStrings.ToList();
                    _header = null;
                }
            }

            EditorGUILayout.EndVertical();

            return typesChanged;
        }

        private IEnumerable<Type> GetSelectedTypes()
        {
            return _selectedTypeStrings.Select(typeString => Type.GetType(typeString)).Where(type => type != null);
        }
    }
}
