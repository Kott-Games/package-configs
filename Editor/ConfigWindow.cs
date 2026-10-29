using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>Config set selector with Validate/Push, the health view and the entry to the template browser.</summary>
    internal sealed class ConfigWindow : EditorWindow
    {
        [SerializeField]
        private ConfigSet _set;

        private readonly ConfigHealthView _health = new();

        /// <summary>Opens the window and validates <paramref name="set" />.</summary>
        public static void ShowValidation(ConfigSet set)
        {
            ConfigWindow window = Open();
            window._set = set;
            window._health.Validate(set);
        }

        [MenuItem("Tools/Configs/Open Window", priority = 0)]
        private static void OpenMenu() => Open();

        [MenuItem("Tools/Configs/Validate All", priority = 20)]
        private static void ValidateAllMenu() => Open()._health.ValidateAll();

        private static ConfigWindow Open()
        {
            ConfigWindow window = GetWindow<ConfigWindow>("Configs", true);
            window.Show();
            return window;
        }

        private void OnGUI()
        {
            List<ConfigSet> sets = ConfigPipeline.FindSets();
            if (!_set || !sets.Contains(_set)) _set = sets.FirstOrDefault();

            if (sets.Count == 0)
            {
                EditorGUILayout.HelpBox("No config set in the project. Create one with Create > Kott > Configs > " +
                                        "Config Set.", MessageType.Info);
                return;
            }

            int index = EditorGUILayout.Popup("Config Set", sets.IndexOf(_set),
                sets.Select(set => set.name).ToArray());
            _set = sets[index];

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate")) _health.Validate(_set);
                if (GUILayout.Button("Validate All")) _health.ValidateAll();
                if (GUILayout.Button("Push")) _health.Show(_set, ConfigPipeline.Push(_set).Issues, "Pushed");
                if (GUILayout.Button("Browse Templates")) TemplateBrowserWindow.Open(_set);
            }

            EditorGUILayout.Space();
            _health.OnGUI();
        }
    }
}
