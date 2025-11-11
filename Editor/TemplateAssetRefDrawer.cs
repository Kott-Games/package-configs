using UnityEditor;
using UnityEngine;

namespace ToG.Config
{
    [CustomPropertyDrawer(typeof(TemplateAssetRef<>))]
    public class TemplateAssetRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty assetProperty = property.FindPropertyRelative("_asset");
            SerializedProperty addressProperty = property.FindPropertyRelative("_address");

            label.tooltip = addressProperty.stringValue;
            EditorGUI.PropertyField(position, assetProperty, label, false);
        }
    }
}
