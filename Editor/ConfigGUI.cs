using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    internal static class ConfigGUI
    {
        public static IEnumerable<SerializedProperty> GetVisibleProperties(SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();

            // property is a real array, so sub properties are not so simple
            if (property.propertyType == SerializedPropertyType.Generic && property.isArray)
            {
                yield return iterator;
                yield break;
            }

            int depth = iterator.depth;
            bool isDone = !iterator.NextVisible(true);

            // no sub properties found
            if (isDone || iterator.depth <= depth)
            {
                yield return property.Copy();
                yield break;
            }

            do
            {
                yield return iterator.Copy();
            }
            while (iterator.NextVisible(false) && iterator.depth > depth);
        }

        public static float GetPropertyCellWidth(SerializedProperty property)
        {
            var propertyInfo = HackedScriptAttributeUtility.GetPropertyInfo(property);

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return 60f;

                case SerializedPropertyType.Enum:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.RenderingLayerMask:
                    return 100f;

                case SerializedPropertyType.String:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector2Int:
                    return 120f;

                // big properties
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Bounds:
                    break;

                // special properties
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float:
                {
                    for (int i = 0; i < propertyInfo.PropertyAttributes.Length; i++)
                    {
                        if (propertyInfo.PropertyAttributes[i] is RangeAttribute rangeAttribute)
                            return 140f;
                    }

                    return 80.0f;
                }
                case SerializedPropertyType.Generic:
                {
                    if (property.type == typeof(TemplateReference<>).Name && !property.isArray)
                        return 150f;
                    return 60f;
                }

                // kind of useless
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Hash128:
                case SerializedPropertyType.Character:
                    break;
            }

            return 60f;
        }

        // TODO add cell drawer attribute to mark complex custom types, so we know we can draw them in a cell
        // TODO add support for field attributes like range on a float
        public static void DrawPropertyCell(Rect position, SerializedProperty property, out bool button)
        {
            button = false;

            var propertyInfo = HackedScriptAttributeUtility.GetPropertyInfo(property);

            switch (property.propertyType)
            {
                // simple properties
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.Gradient:

                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.RenderingLayerMask:

                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector2Int:

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                {
                    EditorGUI.PropertyField(position, property, GUIContent.none, false);
                }
                    break;

                // big properties
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Bounds:
                {
                    button = GUI.Button(position, new GUIContent("Open", property.propertyPath));
                }
                    break;

                // special properties
                case SerializedPropertyType.Boolean:
                {
                    GUIContent icon = property.boolValue
                        ? EditorGUIUtility.IconContent("d_GreenLight")
                        : EditorGUIUtility.IconContent("d_RedLight");

                    Rect iconPosition = new Rect(position) { width = 20f };
                    Rect togglePosition = new Rect(position);
                    togglePosition.width -= iconPosition.width;
                    togglePosition.x += iconPosition.width;

                    GUI.Label(iconPosition, icon);
                    bool toggle = GUI.Button(togglePosition, GUIContent.none);

                    if (toggle) property.boolValue = !property.boolValue;
                }
                    break;
                case SerializedPropertyType.Generic:
                {
                    if (property.type == typeof(TemplateReference<>).Name && !property.isArray)
                    {
                        EditorGUI.PropertyField(position, property, GUIContent.none, false);
                    }
                    else
                    {
                        button = GUI.Button(position, new GUIContent("Open", property.propertyPath));
                    }
                }
                    break;

                // kind of useless
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Hash128:
                case SerializedPropertyType.Character:
                    break;
            }
        }
    }
}
