using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [CustomPropertyDrawer(typeof(TemplateAssetReference<>))]
    public class TemplateAssetReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty assetProperty = property.FindPropertyRelative("_asset");
            SerializedProperty addressProperty = property.FindPropertyRelative("_address");

            label.tooltip = addressProperty.stringValue;
            EditorGUI.PropertyField(position, assetProperty, label, true);
        }
    }
}
