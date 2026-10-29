using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    public class MainConfigWindow : EditorWindow
    {
        [SerializeField]
        private ConfigSet _set;

        [MenuItem("Tools/Configs/Open Window", priority = 0)]
        private static void Open() => GetWindow<MainConfigWindow>("Config Tool", true).Show();

        private void OnGUI()
        {
            List<ConfigSet> sets = ConfigPipeline.FindSets();
            if (!_set || !sets.Contains(_set)) _set = sets.FirstOrDefault();

            if (sets.Count == 0)
            {
                EditorGUILayout.HelpBox("No config set in the project. Create one with Create > Kott > Configs > " +
                                        "Config Set.", MessageType.Info);
            }
            else
            {
                int index = EditorGUILayout.Popup("Config Set", sets.IndexOf(_set),
                    sets.Select(set => set.name).ToArray());
                _set = sets[index];

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Validate")) ConfigSetEditor.Validate(_set);
                if (GUILayout.Button("Push")) ConfigPipeline.Push(_set);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Table")) OpenTable();
        }

        private static void OpenTable()
        {
            TableWindow tableWindow = CreateWindow<TableWindow>();
            tableWindow.UpdateValues("", new ItemFilter());
            tableWindow.Show();
        }
    }
}
