using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Draws a <see cref="TemplateReference{TTemplate}" /> as a single object field. The target must be a template
    ///     asset of the same config set (validation reports anything else).
    /// </summary>
    [CustomPropertyDrawer(typeof(TemplateReference<>))]
    internal sealed class TemplateReferenceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_template"), label, false);
        }
    }
}
