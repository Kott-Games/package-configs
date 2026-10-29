using System;
using System.Collections.Generic;
using System.Linq;
using Kott.Utilities;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Table of the templates of one config set, filtered by type: one row per template, one column per field
    ///     the selected types share. A column header opens that field as its own table.
    /// </summary>
    internal sealed class TemplateBrowserWindow : EditorWindow
    {
        private class Column : EditorTableColumn
        {
            public GUIContent Label;
            public string PropertyPath;
            public bool IsID;
        }

        private class Row : EditorTableRow
        {
            public Template Template;
            public SerializedObject SerializedObject;
        }

        [SerializeField]
        private ConfigSet _set;

        [SerializeField]
        private string _propertyPath;

        [SerializeField]
        private TemplateTypeFilter _filter;

        [SerializeField]
        private EditorTable<Column, Row> _table = new EditorTable<Column, Row>();

        [SerializeField]
        private float _widthID = 120f;

        private SerializedObject _serializedObject;

        private bool _isSetup;
        private Type[] _availableTypes = Array.Empty<Type>();

        private bool _stylesCached;
        private GUIContent _headerButtonLabel;
        private GUIStyle _headerButtonStyle;

        /// <summary>Opens a new browser over the templates of <paramref name="set" />.</summary>
        public static void Open(ConfigSet set)
        {
            TemplateBrowserWindow window = CreateWindow<TemplateBrowserWindow>();
            window.UpdateValues(set, "", new TemplateTypeFilter());
            window.Show();
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);

            Setup();
        }

        private void OnDisable()
        {
            _serializedObject?.Dispose();
            _serializedObject = null;

            Cleanup();
        }

        private void UpdateValues(ConfigSet set, string propertyPath, TemplateTypeFilter filter)
        {
            _set = set;
            _propertyPath = propertyPath;
            _filter = filter;
            Setup();
        }

        private void Setup()
        {
            Cleanup();

            if (!_set) return;
            if (_filter == null) return;
            if (_propertyPath == null) return;

            titleContent = new GUIContent(string.IsNullOrEmpty(_propertyPath)
                ? $"{_set.name} Templates"
                : $"{_set.name} {_propertyPath}");

            List<Template> setTemplates = ConfigPipeline.FindTemplates(_set);
            _availableTypes = setTemplates.Select(template => template.GetType()).Distinct().ToArray();

            Template[] templates = _filter.Filter(setTemplates).ToArray();
            if (templates.Length == 0) return;

            _table.Setup(TableDrawHeader, TableDrawCell);
            _table.HeaderHeight = 24f;
            _table.RowHeight = 18f;
            _table.ColumnSpacing = 6f;
            _table.RowSpacing = 3f;
            _table.LockHeader = true;
            _table.LockFirstColumn = true;
            _table.CullCells = true;

            for (int i = 0; i < templates.Length; i++)
            {
                _table.Rows.Add(new Row()
                {
                    Template = templates[i],
                    SerializedObject = new SerializedObject(templates[i]),
                });
            }

            _table.Columns.Add(new Column()
            {
                Width = 120f,
                Label = new GUIContent("ID"),
                PropertyPath = "ID",
                IsID = true,
            });

            HashSet<Type> templateTypes = new();
            Dictionary<string, Column> possibleColumns = new();
            HashSet<string> propertyPaths = null;

            foreach (var row in _table.Rows)
            {
                if (!templateTypes.Add(row.Template.GetType()))
                    continue;

                SerializedProperty rootProperty = string.IsNullOrEmpty(_propertyPath)
                    ? row.SerializedObject.GetIterator()
                    : row.SerializedObject.FindProperty(_propertyPath);

                if (propertyPaths == null)
                {
                    propertyPaths = new();
                    foreach (var property in ConfigGUI.GetVisibleProperties(rootProperty))
                    {
                        if (property.propertyPath == "m_Script") continue;
                        if (property.propertyPath == "ID") continue;

                        propertyPaths.Add(property.propertyPath);
                        possibleColumns.Add(property.propertyPath, new Column()
                        {
                            Width = ConfigGUI.GetPropertyCellWidth(property),
                            Label = new GUIContent(property.displayName, property.displayName),
                            PropertyPath = property.propertyPath,
                        });
                    }
                }
                else
                {
                    propertyPaths.IntersectWith(
                        ConfigGUI.GetVisibleProperties(rootProperty).Select(p => p.propertyPath));
                }
            }

            foreach (var propertyPath in propertyPaths)
            {
                _table.Columns.Add(possibleColumns[propertyPath]);
            }

            _isSetup = true;
        }

        private void Cleanup()
        {
            for (int i = 0; i < _table.Rows.Count; i++)
                _table.Rows[i].SerializedObject?.Dispose();

            _table.Columns.Clear();
            _table.Rows.Clear();

            _isSetup = false;
            _availableTypes = Array.Empty<Type>();
        }

        private void OnGUI()
        {
            if (!_stylesCached)
            {
                _stylesCached = true;
                _headerButtonLabel = EditorGUIUtility.IconContent("d_BuildSettings.N3DS@2x");
                _headerButtonStyle = new GUIStyle(EditorStyles.iconButton);
            }

            if (!_set || _filter == null)
            {
                EditorGUILayout.HelpBox("The config set is gone. Open the browser again from the config window.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(_set, typeof(ConfigSet), false);
                }

                if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false))) Setup();
            }

            if (!string.IsNullOrEmpty(_propertyPath))
            {
                EditorGUILayout.LabelField("Property Path", _propertyPath, EditorStyles.boldLabel);
            }

            GUILayout.Space(3.0f);

            bool filterChanged = _filter.DrawGUILayout(_availableTypes);
            if (filterChanged)
            {
                Setup();
                GUIUtility.ExitGUI();
            }

            if (!_isSetup)
            {
                EditorGUILayout.HelpBox("No templates of the selected types in this config set.", MessageType.Info);
                return;
            }

            _serializedObject.Update();

            GUILayout.Space(3.0f);

            SerializedProperty propertyWidthID = _serializedObject.FindProperty("_widthID");
            EditorGUILayout.PropertyField(propertyWidthID);
            propertyWidthID.floatValue = Mathf.Max(60f, propertyWidthID.floatValue);

            GUILayout.Space(3.0f);

            _table.Columns[0].Width = _widthID;

            for (int i = 0; i < _table.Rows.Count; i++)
                _table.Rows[i].SerializedObject.Update();

            _table.DrawGUILayoutTable();

            for (int i = 0; i < _table.Rows.Count; i++)
                _table.Rows[i].SerializedObject.ApplyModifiedProperties();

            _serializedObject.ApplyModifiedProperties();
        }

        private void TableDrawHeader(Rect position, Column column)
        {
            CustomGUI.DrawBox(position, Color.gray);

            Rect buttonPosition = new Rect(position);
            buttonPosition.width = 20.0f;
            buttonPosition.x = position.x + position.width - buttonPosition.width;

            Rect headerPosition = new Rect(position);
            headerPosition.width -= buttonPosition.width;

            // TODO increase height and write two labels over each other (name + type)
            GUI.Label(headerPosition, column.Label, EditorStyles.boldLabel);
            bool button = GUI.Button(buttonPosition, _headerButtonLabel, _headerButtonStyle);

            if (button)
            {
                TemplateBrowserWindow window = CreateWindow<TemplateBrowserWindow>();
                window.UpdateValues(_set, column.PropertyPath, new TemplateTypeFilter(_filter));
                window.Show();
            }
        }

        private void TableDrawCell(Rect position, Column column, Row row)
        {
            if (column.IsID) TableDrawCellID(position, column, row);
            else TableDrawCellProperty(position, column, row);
        }

        private void TableDrawCellID(Rect position, Column column, Row row)
        {
            SerializedProperty property = row.SerializedObject.FindProperty(column.PropertyPath);

            bool button = GUI.Button(position, GUIContent.none, EditorStyles.helpBox);

            position = position.AddPadding(3f, 0f, 3f, 0f);
            GUI.Label(position, property.stringValue, EditorStyles.boldLabel);

            if (button)
            {
                TemplateFieldWindow.Open(_propertyPath, row.Template);
            }
        }

        private void TableDrawCellProperty(Rect position, Column column, Row row)
        {
            SerializedProperty property = row.SerializedObject.FindProperty(column.PropertyPath);
            if (property == null) return;

            ConfigGUI.DrawPropertyCell(position, property, out bool button);

            if (button)
            {
                TemplateFieldWindow.Open(column.PropertyPath, row.Template);
            }
        }
    }
}
