using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Kott.Configs
{
    [CustomPropertyDrawer(typeof(TemplateReference<>))]
    public class TemplateReferenceDrawer : PropertyDrawer
    {
        private Type _type;
        private Type[] _types;
        private GUIContent[] _typeLabels;

        private readonly NamingStrategy _namingStrategy = new SnakeCaseNamingStrategy();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheTypes(property);

            Rect buttonRect = position;
            buttonRect.width = 48f;
            position.width -= buttonRect.width + 2f;
            buttonRect.x += position.width + 2f;

            SerializedProperty templateProperty = property.FindPropertyRelative("_template");
            EditorGUI.PropertyField(position, templateProperty, label, true);

            Type createType = HandleTypeDropdown(buttonRect);
            if (createType != null)
            {
                Debug.Log($"Create new instance of {createType.Name}");
                CreateSubAsset(property, templateProperty, createType);
            }
        }

        private Type HandleTypeDropdown(Rect position)
        {
            if (_types.Length < 1) return null;
            if (_types.Length == 1)
            {
                bool button = GUI.Button(position, "Create");
                return button ? _types[0] : null;
            }

            int index = EditorGUI.Popup(position, GUIContent.none, -1, _typeLabels);

            // invalid or special case <none>
            if (index <= 0 || index > _typeLabels.Length) return null;

            Type createType = _types[index - 1];
            return createType;
        }

        private void CacheTypes(SerializedProperty property)
        {
            Type type = property.boxedValue.GetType().GetGenericArguments()[0];
            if (_type != null && _type.IsEquivalentTo(type)) return;
            _type = type;

            _types ??= TypeCache
                .GetTypesDerivedFrom(typeof(object))
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => type.IsAssignableFrom(t))
                .ToArray();

            _typeLabels ??= _types
                .Select(t => new GUIContent(t.Name))
                .Prepend(new GUIContent("<none>"))
                .ToArray();
        }

        private void CreateSubAsset(SerializedProperty property, SerializedProperty templateProperty, Type type)
        {
            UObject asset = property.serializedObject.targetObject;
            UObject subAsset = ScriptableObject.CreateInstance(type);

            const bool UsePrefix = true;

            string name = _namingStrategy.GetPropertyName(property.displayName, false);
            subAsset.name = UsePrefix ? $"{asset.name}_{name}" : name;
            subAsset.hideFlags = HideFlags.None;

            UObject previousAsset = templateProperty.objectReferenceValue;

            // copy property values
            if (previousAsset)
                EditorUtility.CopySerialized(previousAsset, subAsset);

            // delete unused subasset
            if (previousAsset && AssetDatabase.IsSubAsset(previousAsset))
            {
                AssetDatabase.RemoveObjectFromAsset(previousAsset);
                UObject.DestroyImmediate(previousAsset);
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetDatabase.AddObjectToAsset(subAsset, assetPath);

            templateProperty.objectReferenceValue = subAsset;
        }
    }
}
