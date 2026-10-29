using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [CustomPropertyDrawer(typeof(SubtypePickerAttribute))]
    internal sealed class SubtypePickerDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Rect buttonPosition = new(position.x + EditorGUIUtility.labelWidth + 2f, position.y,
                position.width - EditorGUIUtility.labelWidth - 2f, EditorGUIUtility.singleLineHeight);
            object value = property.managedReferenceValue;
            GUIContent typeLabel = new(value == null ? "None" : ObjectNames.NicifyVariableName(value.GetType().Name));
            if (EditorGUI.DropdownButton(buttonPosition, typeLabel, FocusType.Keyboard))
            {
                ShowTypeMenu(property, value?.GetType());
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        private void ShowTypeMenu(SerializedProperty property, Type currentType)
        {
            Type baseType = GetElementType(fieldInfo.FieldType);
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("None"), currentType == null, () => SetValue(property, null));

            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType)
                         .Append(baseType)
                         .Where(IsInstantiable)
                         .OrderBy(type => type.Name))
            {
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), type == currentType,
                    () => SetValue(property, Activator.CreateInstance(type)));
            }

            menu.ShowAsContext();
        }

        private static void SetValue(SerializedProperty property, object value)
        {
            property.serializedObject.Update();
            property.managedReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static Type GetElementType(Type fieldType)
        {
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldType))
            {
                return fieldType.GetGenericArguments()[0];
            }

            return fieldType;
        }

        private static bool IsInstantiable(Type type)
        {
            return !type.IsAbstract && !type.IsGenericTypeDefinition &&
                   !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                   type.IsDefined(typeof(SerializableAttribute), false) &&
                   type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null) !=
                   null;
        }
    }
}
