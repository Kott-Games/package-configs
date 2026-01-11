using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [CustomPropertyDrawer(typeof(TemplateReference<>))]
    public class TemplateReferenceDrawer : PropertyDrawer
    {
        // private GUIStyle _buttonStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty templateProperty = property.FindPropertyRelative("_template");
            EditorGUI.PropertyField(position, templateProperty, label, true);

            // if (_buttonStyle == null)
            // {
            //     _buttonStyle = new GUIStyle(EditorStyles.helpBox);
            //     _buttonStyle.wordWrap = false;
            // }
            // 
            // position = EditorGUI.PrefixLabel(position, label);
            // 
            // Rect buttonPosition = new Rect(position);
            // Rect pingPosition = new Rect(position);
            // 
            // pingPosition.width = 28f;
            // buttonPosition.width -= pingPosition.width;
            // pingPosition.x += buttonPosition.width;
            // 
            // SerializedProperty assetProperty = property.FindPropertyRelative("_asset");
            // GUIContent buttonLabel = EditorGUIUtility.ObjectContent(assetProperty.objectReferenceValue, typeof(Template));
            // if (assetProperty.objectReferenceValue != null)
            // {
            //     buttonLabel = new GUIContent(buttonLabel);
            //     buttonLabel.tooltip = buttonLabel.text;
            //     buttonLabel.text = assetProperty.objectReferenceValue.name;
            // }
            // 
            // if (GUI.Button(buttonPosition, buttonLabel, _buttonStyle))
            // {
            //     Type type = property.boxedValue.GetType().GetGenericArguments()[0];
            //     string filter = $"l:{ConfigEditorUtility.TypeToLabel(type)}";
            //     EditorGUIUtility.ShowObjectPicker<Template>(assetProperty.objectReferenceValue, false, filter, 69);
            //     property.isExpanded = true;
            // }
            // 
            // if (GUI.Button(pingPosition, new GUIContent("°_°")))
            // {
            //     EditorGUIUtility.PingObject(assetProperty.objectReferenceValue);
            // }
            // 
            // if (EditorGUIUtility.GetObjectPickerControlID() == 69 && property.isExpanded)
            // {
            //     Event eCurrent = Event.current;
            //     if (eCurrent.type == EventType.ExecuteCommand)
            //     {
            //         bool update = eCurrent.commandName == "ObjectSelectorUpdated";
            //         bool close = eCurrent.commandName == "ObjectSelectorClosed";
            //         if (update || close)
            //         {
            //             assetProperty.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
            //             eCurrent.Use();
            // 
            //             if (close)
            //             {
            //                 property.isExpanded = false;
            //             }
            //         }
            //     }
            // }
            // else
            // {
            //     property.isExpanded = false;
            // }
        }
    }
}
