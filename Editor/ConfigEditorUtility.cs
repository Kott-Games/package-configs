using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kott.Configs.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    public class ConfigEditorUtility
    {
        private static Dictionary<Type, string> _typeDisplayNames = new();

        public static string TypeToLabel(Type type)
        {
            string name = type.AssemblyQualifiedName;

            int hash = 0;
            for (int i = 0; i < name.Length; i++)
            {
                hash += name[i] ^ 86889427;
                hash *= 41200111;
                hash ^= 22245263;
            }

            return hash.ToString();
        }

        public static IEnumerable<Template> GetTemplatesAll()
        {
            return GetTemplatesWithType(typeof(Template));
        }

        public static IEnumerable<Template> GetTemplatesWithType(Type type)
        {
            return AssetDatabase.FindAssets($"t:{type.FullName}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .SelectMany(assetPath => AssetDatabase.LoadAllAssetsAtPath(assetPath))
                .Select(asset => asset as Template)
                .Where(template => template);
        }

        public static IEnumerable<T> GetTemplatesWithType<T>()
            where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).FullName}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .SelectMany(assetPath => AssetDatabase.LoadAllAssetsAtPath(assetPath))
                .Select(asset => asset as T)
                .Where(template => template);
        }

        public static T GetConfigDataCache<T>()
            where T : ScriptableObject
        {
            string assetPath = $"Assets/Configs/{typeof(T).Name}.asset";
            T dataCache = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (dataCache == null)
            {
                dataCache = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(dataCache, assetPath);
            }

            return dataCache;
        }

        public static string GetTypeDisplayName(Type type)
        {
            if (!_typeDisplayNames.TryGetValue(type, out string displayName))
            {
                TemplateGroupAttribute groupAttribute = type.GetCustomAttribute<TemplateGroupAttribute>();
                displayName = groupAttribute != null ? groupAttribute.DisplayName : type.Name;
                _typeDisplayNames.Add(type, displayName);
            }

            return displayName;
        }
    }
}
