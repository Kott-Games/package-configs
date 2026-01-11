using Kott.Utilities;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    public class ItemWindow : EditorWindow
    {
        private class Column : EditorTableColumn
        {
            public GUIContent Label;
            public string RelativePropertyPath;
            public bool IsControl;
        }

        private class Row : EditorTableRow
        {
            public int ArrayIndex;
            public Vector2 OffsetVelocity;
        }

        private struct RenderOnTopData
        {
            public Rect Position;
            public Column Column;
        }

        [SerializeField] private string _propertyPath;
        [SerializeField] private Template _template;
        [SerializeField] private EditorTable<Column, Row> _table = new EditorTable<Column, Row>();

        private SerializedObject _serializedObject;

        private bool _stylesCached;
        private GUIContent _headerButtonLabel;
        private GUIStyle _headerButtonStyle;

        private int _moveStartIndex = -1;
        private int _moveStopIndex = -1;
        private int _deleteIndex = -1;

        private void OnEnable()
        {
            Setup();
        }

        private void OnDisable()
        {
            _serializedObject?.Dispose();
            _serializedObject = null;
        }

        public void UpdateValues(string propertyPath, Template wrapper)
        {
            _propertyPath = propertyPath;
            _template = wrapper;
            Setup();
        }

        private void Setup()
        {
            Cleanup();

            if (_propertyPath == null) return;
            if (_template == null) return;

            _serializedObject = new SerializedObject(_template);

            _table.Setup(TableDrawHeader, TableDrawCell);
            _table.HeaderHeight = 24f;
            _table.RowHeight = 18f;
            _table.ColumnSpacing = 6f;
            _table.RowSpacing = 3f;
            _table.LockHeader = true;
            _table.LockFirstColumn = true;
            _table.CullCells = true;

            _table.Columns.Add(new Column()
            {
                Width = 120f,
                Label = new GUIContent("Controls"),
                IsControl = true,
            });

            SerializedProperty rootProperty = string.IsNullOrEmpty(_propertyPath) ?
                _serializedObject.GetIterator() :
                _serializedObject.FindProperty(_propertyPath);

            if (rootProperty.propertyType == SerializedPropertyType.Generic && rootProperty.isArray)
            {
                bool hasArrayElements = rootProperty.arraySize > 0;
                if (!hasArrayElements)
                    rootProperty.arraySize = 1;

                SerializedProperty templateProperty = rootProperty.GetArrayElementAtIndex(0);
                if (templateProperty.type == typeof(TemplateReference<>).Name && !templateProperty.isArray)
                {
                    _table.Columns.Add(new Column()
                    {
                        Width = ConfigGUI.GetPropertyCellWidth(templateProperty),
                        Label = new GUIContent(templateProperty.type, templateProperty.type),
                        RelativePropertyPath = null,
                    });
                }
                else
                {
                    foreach (SerializedProperty subProperty in ConfigGUI
                        .GetVisibleProperties(templateProperty))
                    {
                        _table.Columns.Add(new Column()
                        {
                            Width = ConfigGUI.GetPropertyCellWidth(subProperty),
                            Label = new GUIContent(subProperty.displayName, subProperty.displayName),
                            RelativePropertyPath = subProperty.name,
                        });
                    }

                    if (_table.Columns.Count == 2)
                    {
                        _table.Columns[1].Label = new GUIContent(templateProperty.type, templateProperty.type);
                        _table.Columns[1].RelativePropertyPath = null;
                    }
                }

                if (!hasArrayElements)
                    rootProperty.arraySize = 0;

                for (int i = 0; i < rootProperty.arraySize; i++)
                {
                    SerializedProperty itemProperty = rootProperty.GetArrayElementAtIndex(i);

                    _table.Rows.Add(new Row()
                    {
                        ArrayIndex = i,
                    });
                }
            }
        }

        private void Cleanup()
        {
            _serializedObject?.Dispose();
            _serializedObject = null;
        }

        private void Update()
        {
            bool hasOffset = false;
            for (int i = 0; i < _table.Rows.Count; i++)
            {
                Row row = _table.Rows[i];
                if (row.PositionOffset.sqrMagnitude < 0.1f)
                {
                    row.DrawOnTop = false;
                    row.OffsetVelocity = Vector2.zero;
                    row.PositionOffset = Vector2.zero;
                }
                else
                {
                    hasOffset = true;
                    row.PositionOffset = Vector2.SmoothDamp(
                        row.PositionOffset, Vector2.zero, ref row.OffsetVelocity, 3f, 100f, Time.deltaTime);
                }
            }

            if (hasOffset)
                Repaint();
        }

        private void OnGUI()
        {
            if (!_stylesCached)
            {
                _stylesCached = true;
                _headerButtonLabel = EditorGUIUtility.IconContent("d_BuildSettings.N3DS@2x");
                _headerButtonStyle = new GUIStyle(EditorStyles.iconButton);
            }

            EditorGUILayout.LabelField("Property Path", _propertyPath, EditorStyles.boldLabel);
            GUILayout.Space(3.0f);

            if (_serializedObject == null)
            {
                EditorGUILayout.HelpBox("Nothing to show.", MessageType.Info);
                return;
            }

            bool guiEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.ObjectField(GUIContent.none, _template, typeof(Template), false);
            GUI.enabled = guiEnabled;

            GUILayout.Space(3.0f);

            _serializedObject.Update();

            SerializedProperty rootProperty = string.IsNullOrEmpty(_propertyPath) ?
                _serializedObject.GetIterator() :
                _serializedObject.FindProperty(_propertyPath);

            if (rootProperty.propertyType == SerializedPropertyType.Generic && rootProperty.isArray)
            {
                DrawArrayProperty(rootProperty);
            }
            else if (rootProperty.propertyType == SerializedPropertyType.Generic &&
                rootProperty.type != typeof(TemplateReference<>).Name ||
                rootProperty.propertyType == SerializedPropertyType.Vector4)
            {
                foreach (SerializedProperty subProperty in ConfigGUI
                    .GetVisibleProperties(rootProperty))
                {
                    if (subProperty.propertyPath == "m_Script") continue;
                    if (subProperty.propertyPath == "ID") continue;

                    EditorGUILayout.PropertyField(subProperty, true);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(rootProperty, GUIContent.none, false);
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawArrayProperty(SerializedProperty property)
        {
            GUILayout.BeginHorizontal();
            bool addButton = GUILayout.Button(new GUIContent("Add Item"), GUILayout.ExpandWidth(false));
            bool clearButton = GUILayout.Button(new GUIContent("Clear All"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.Space(3.0f);

            EventType eventType = Event.current.type;

            if (_table.Rows.Count != property.arraySize)
            {
                for (int i = _table.Rows.Count; i < property.arraySize; i++)
                    _table.Rows.Add(new Row());
                for (int i = _table.Rows.Count - 1; i >= property.arraySize; i--)
                    _table.Rows.RemoveAt(i);
                for (int i = 0; i < _table.Rows.Count; i++)
                    _table.Rows[i].ArrayIndex = i;
            }

            _table.DrawGUILayoutTable();

            if (addButton)
            {
                property.arraySize++;
                _table.Rows.Add(new Row()
                {
                    ArrayIndex = _table.Rows.Count,
                });
            }
            else if (clearButton)
            {
                property.arraySize = 0;
            }
            else if (_deleteIndex >= 0)
            {
                property.DeleteArrayElementAtIndex(_deleteIndex);
                _table.Rows.RemoveAt(_table.Rows.Count - 1);
                _deleteIndex = -1;
            }
            else if (_moveStartIndex >= 0 && _moveStopIndex >= 0)
            {
                GUI.FocusControl("");

                property.MoveArrayElement(_moveStartIndex, _moveStopIndex);

                float rowOffset = _table.RowHeight + _table.RowSpacing;

                for (int i = _moveStartIndex; i < _moveStopIndex; i++)
                {
                    _table.Rows[i].OffsetVelocity = Vector2.zero;
                    _table.Rows[i].PositionOffset = new Vector2(0f, rowOffset);
                }

                for (int i = _moveStopIndex + 1; i <= _moveStartIndex; i++)
                {
                    _table.Rows[i].OffsetVelocity = Vector2.zero;
                    _table.Rows[i].PositionOffset = new Vector2(0f, -rowOffset);
                }

                _table.Rows[_moveStopIndex].DrawOnTop = true;
                _table.Rows[_moveStopIndex].OffsetVelocity = Vector2.zero;
                _table.Rows[_moveStopIndex].PositionOffset =
                    new Vector2(0f, (_moveStartIndex - _moveStopIndex) * rowOffset);

                _moveStartIndex = -1;
                _moveStopIndex = -1;
            }
            else if (eventType != EventType.Repaint && eventType != EventType.Layout &&
                eventType != EventType.MouseDown && eventType != EventType.MouseDrag &&
                eventType != EventType.ScrollWheel)
            {
                _moveStartIndex = -1;
                _moveStopIndex = -1;
            }
        }

        private void TableDrawHeader(Rect position, Column column)
        {
            CustomGUI.DrawBox(position, Color.gray);

            // TODO increase height and write two labels over each other (name + type)
            GUI.Label(position, column.Label, EditorStyles.boldLabel);
        }

        private void TableDrawCell(Rect position, Column column, Row row)
        {
            if (column.IsControl) TableDrawCellControl(position, row);
            else TableDrawCellProperty(position, column, row);
        }

        private void TableDrawCellControl(Rect position, Row row)
        {
            Rect movePosition = new Rect(position);
            Rect deletePosition = new Rect(position);

            movePosition.width *= 0.5f;
            deletePosition.width -= movePosition.width;
            deletePosition.x += movePosition.width;

            Event current = Event.current;
            if (current.type == EventType.MouseDown && movePosition.Contains(current.mousePosition))
            {
                _moveStartIndex = row.ArrayIndex;
                _moveStopIndex = -1;
                current.Use();
            }
            else if (current.type == EventType.MouseUp && movePosition.Contains(current.mousePosition))
            {
                _moveStopIndex = row.ArrayIndex;
                current.Use();
            }

            Color backgroundColor = GUI.color;
            GUI.color = _moveStartIndex == row.ArrayIndex ? Color.yellow :
                _moveStartIndex >= 0 && movePosition.Contains(current.mousePosition) ? Color.green : backgroundColor;
            bool buttonMove = GUI.Button(movePosition, new GUIContent("Move"));
            GUI.color = backgroundColor;

            bool deleteButton = GUI.Button(deletePosition, new GUIContent("Delete"));
            if (deleteButton) _deleteIndex = row.ArrayIndex;
        }

        private void TableDrawCellProperty(Rect position, Column column, Row row)
        {
            SerializedProperty rootProperty = string.IsNullOrEmpty(_propertyPath) ?
                _serializedObject.GetIterator() :
                _serializedObject.FindProperty(_propertyPath);

            SerializedProperty itemProperty = rootProperty.GetArrayElementAtIndex(row.ArrayIndex);
            SerializedProperty property = column.RelativePropertyPath != null ?
                itemProperty.FindPropertyRelative(column.RelativePropertyPath) : itemProperty;

            ConfigGUI.DrawPropertyCell(position, property, out bool button);

            if (button)
            {
                ItemWindow itemWindow = CreateWindow<ItemWindow>();
                string subPath = column.RelativePropertyPath != null ?
                    $"{_propertyPath}.Array.data[{row.ArrayIndex}].{column.RelativePropertyPath}" :
                    $"{_propertyPath}.Array.data[{row.ArrayIndex}]";
                itemWindow.UpdateValues(subPath, _template);
                itemWindow.Show();
            }
        }
    }
}
