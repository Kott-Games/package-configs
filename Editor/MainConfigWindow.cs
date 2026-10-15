using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    public class MainConfigWindow : EditorWindow
    {
        [MenuItem("Tools/Configs/Open Window", priority = 0)]
        private static void Open() => GetWindow<MainConfigWindow>("Config Tool", true).Show();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Update")) UpdateConfigs();
            if (GUILayout.Button("Export")) ExportConfigs();
            if (GUILayout.Button("Copy to Clipboard")) CopyConfigToClipboard();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Update, Export and Copy to Clipboard")) UpdateExportCopyConfigToClipboard();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Table")) OpenTable();
        }

        private static void UpdateConfigs()
        {
            Debug.Log($"[Config] Update");
            ConfigStructureUtility.UpdateConfigStructure();
        }

        private static void ExportConfigs()
        {
            Debug.Log($"[Config] Export");

            ConfigTypeMapping configTypeMapping = ConfigEditorUtility.GetConfigDataCache<ConfigTypeMapping>();
            RuntimeConfigData runtimeConfigData = ConfigEditorUtility.GetConfigDataCache<RuntimeConfigData>();

            JArray jTemplates = ConfigSerializationUtility.Serialize(configTypeMapping, runtimeConfigData.Templates);
            JObject entries = new(new JProperty(ConfigVersionUtility.GetTemplateDataKey(), jTemplates));

            RCJsonAPI.WriteToConfigAsset("json_config", entries);
        }

        private static void UpdateExportCopyConfigToClipboard()
        {
            UpdateConfigs();
            ExportConfigs();
            CopyConfigToClipboard();
        }

        private static void CopyConfigToClipboard()
        {
            Debug.Log($"[Config] Copy to Clipboard");
            JObject entries = RCJsonAPI.ReadFromConfigAsset("json_config");
            string payload = entries.ToString(Formatting.None);
            GUIUtility.systemCopyBuffer = payload;
        }

        private static void OpenTable()
        {
            TableWindow tableWindow = CreateWindow<TableWindow>();
            tableWindow.UpdateValues("", new ItemFilter());
            tableWindow.Show();
        }
    }
}
