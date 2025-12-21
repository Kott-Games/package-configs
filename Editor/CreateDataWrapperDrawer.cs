using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [CustomPropertyDrawer(typeof(CreateDataWrapper<>))]
    public class CreateDataWrapperDrawer : PropertyDrawer
    {
        private Type _type;
        private Type[] _types;
        private GUIContent[] _typeLabels;
        private GUIContent _popupLabel;

        private const float BoxPadding = 3f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("_value");
            return EditorGUI.GetPropertyHeight(valueProperty, true) +
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                2f * BoxPadding;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheTypes(property);

            Rect boxPosition = EditorGUI.IndentedRect(position);
            GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);

            position.position += BoxPadding * Vector2.one;
            position.size -= 2f * BoxPadding * Vector2.one;

            Rect headerPosition = position;
            headerPosition.height = EditorGUIUtility.singleLineHeight;

            Rect valuePosition = position;
            valuePosition.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            valuePosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty valueProperty = property.FindPropertyRelative("_value");
            HandleTypeDropdown(headerPosition, valueProperty);
            EditorGUI.PropertyField(valuePosition, valueProperty, true);
        }
        
        private void HandleTypeDropdown(Rect position, SerializedProperty valueProperty)
        {
            int index = EditorGUI.Popup(position, _popupLabel, -1, _typeLabels);

            // invalid or special case <none>
            if (index <= 0 || index > _typeLabels.Length) return;

            Type createType = _types[index - 1];
            object value = Activator.CreateInstance(createType);
            valueProperty.managedReferenceValue = value;
        }

        private void CacheTypes(SerializedProperty property)
        {
            Type type = property.boxedValue.GetType().GetGenericArguments()[0];
            if (_type != null && _type.IsEquivalentTo(type)) return;
            _type = type;

            /**
            _types ??= TypeCache
                .GetTypesDerivedFrom(type)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .ToArray();
            **/

            _types ??= TypeCache
                .GetTypesDerivedFrom(typeof(object))
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => type.IsAssignableFrom(t))
                .ToArray();

            _typeLabels ??= _types
                .Select(t => new GUIContent(t.Name))
                .Prepend(new GUIContent("<none>"))
                .ToArray();

            _popupLabel = new GUIContent(type.Name);
        }
    }
}
