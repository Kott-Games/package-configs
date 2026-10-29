using System.Linq;
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
                if (GUILayout.Button("Validate")) Validate(set);
                if (GUILayout.Button("Push")) ConfigPipeline.Push(set);
            }
        }

        internal static void Validate(ConfigSet set)
        {
            ConfigBuildResult result = ConfigPipeline.Build(set);
            ConfigPipeline.LogIssues(result.Issues);
            int errors = result.Issues.Count(issue => issue.Severity == ConfigIssueSeverity.Error);
            Debug.Log($"[Configs] Validated '{set.name}': {result.TemplateCount} templates, {errors} errors, " +
                      $"{result.Issues.Count - errors} warnings.", set);
        }
    }
}
