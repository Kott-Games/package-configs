using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToG.Config
{
    public static class RCJsonAPI
    {
        public static void WriteToConfigAsset(string name, JObject entries)
        {
            string directoryPath = Path.Combine(Application.dataPath, "Configs");
            string filePath = Path.Combine(directoryPath, $"{name}.rc");
            string assetPath = $"Assets/Configs/{name}.rc";

            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                StreamWriter fs = File.CreateText(filePath);
                fs.Close();
            }

            JObject data = new JObject()
            {
                new JProperty("$schema", "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json"),
                new JProperty("entries", entries),
            };

            File.WriteAllText(filePath, data.ToString());
            AssetDatabase.ImportAsset(assetPath);
        }

        public static JObject ReadFromConfigAsset(string name)
        {
            string directoryPath = Path.Combine(Application.dataPath, "Configs");
            string filePath = Path.Combine(directoryPath, $"{name}.rc");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Cannot read from config asset.", filePath);
            }

            JObject data = JObject.Parse(File.ReadAllText(filePath));
            JObject entries = data.TryGetValue("entries", out JToken token) ? token as JObject : null;
            if (entries == null)
            {
                throw new KeyNotFoundException("No entries found.");
            }

            return entries;
        }
    }
}
