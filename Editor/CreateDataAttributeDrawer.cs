using Kott.Scopes;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [CustomPropertyDrawer(typeof(CreateDataWrapper<>))]
    public class CreateDataAttributeDrawer : PropertyDrawer
    {
        private Type[] _types;
        private string[] _typeNames;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded) return height;

            height += EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight;

            SerializedProperty propertyIterator = property;
            int depth = propertyIterator.depth;
            while (propertyIterator.NextVisible(propertyIterator.depth <= depth) &&
                propertyIterator.depth > depth)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(propertyIterator);
            }

            return height;
        }


        // TODO update to support generic types properly
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheTypes();

            Rect subPosition = position;

            subPosition.height = EditorGUIUtility.singleLineHeight;

            Type typeCurrent = property.boxedValue?.GetType();
            Type typeNext = DrawTypeDropdown(subPosition, typeCurrent);
            if (typeCurrent != typeNext)
            {
                property.boxedValue = typeNext != null ? Activator.CreateInstance(typeNext) : null;
                typeCurrent = typeNext;
            }

            if (property.boxedValue is not Manager manager) return;

            subPosition.y += subPosition.height;
            subPosition.y += EditorGUIUtility.standardVerticalSpacing;
            subPosition.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(subPosition, typeCurrent.Name, EditorStyles.boldLabel);

            subPosition.y += subPosition.height;
            subPosition.y += EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty propertyIterator = property;
            int depth = propertyIterator.depth;
            while (propertyIterator.NextVisible(propertyIterator.depth <= depth) &&
                propertyIterator.depth > depth)
            {
                subPosition.height = EditorGUI.GetPropertyHeight(propertyIterator);

                EditorGUI.PropertyField(subPosition, propertyIterator, true);

                subPosition.y += subPosition.height;
                subPosition.y += EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private Type DrawTypeDropdown(Rect position, Type type)
        {
            int index = EditorGUI.Popup(position, "Create", -1, _typeNames);
            if (index == 0) return null; // special case <none>
            if (index > 0 && index <= _typeNames.Length) type = _types[index - 1];
            return type;
        }

        private void CacheTypes()
        {
            _types ??= TypeCache
                .GetTypesDerivedFrom<Manager>()
                .Where(type => !type.IsAbstract)
                .ToArray();

            _typeNames ??= _types
                .Select(type => type.Name)
                .Prepend("<none>")
                .ToArray();
        }
    }
}
