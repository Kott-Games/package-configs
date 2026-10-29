using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    [CustomEditor(typeof(ConfigSet))]
    internal sealed class ConfigSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            ConfigSet set = (ConfigSet)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate")) ConfigWindow.ShowValidation(set);
                if (GUILayout.Button("Push")) ConfigPipeline.Push(set);
            }
        }
    }
}
