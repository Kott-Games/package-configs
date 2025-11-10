using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kott.Configs.Attributes;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Kott.Configs
{
    public class AssetCheckerWindow : EditorWindow
    {
        private class AssetInfo
        {
            public string Key;
            public bool IsOptional;
            public Type AssetType;
            public UnityEngine.Object Asset;
        }

        private class ItemAssetInfo
        {
            public Template Template;
            public List<AssetInfo> Infos = new();
            public int CountTotalRequired;
            public int CountMissingRequired;
            public int CountTotalOptional;
            public int CountMissingOptional;
        }

        [SerializeField] private bool _hideAvailable = false;
        [SerializeField] private bool _hideOptional = false;

        private SerializedObject _serializedObject;

        private List<ItemAssetInfo> ItemInfos = new();
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);

            CheckAssets();
        }

        private void OnDisable()
        {
            _serializedObject?.Dispose();
            _serializedObject = null;
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Check Assets")) CheckAssets();

            GUILayout.Space(3.0f);

            _serializedObject.Update();

            EditorGUILayout.PropertyField(_serializedObject.FindProperty("_hideAvailable"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("_hideOptional"));

            _serializedObject.ApplyModifiedProperties();

            GUILayout.Space(6.0f);

            Color guiBackgroundColor = GUI.backgroundColor;

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (ItemAssetInfo itemInfo in ItemInfos)
            {
                if (itemInfo.Infos.Count <= 0) continue;
                if (_hideAvailable && itemInfo.CountMissingRequired + itemInfo.CountMissingOptional <= 0) continue;
                if (_hideOptional && itemInfo.CountTotalRequired <= 0) continue;

                GUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUILayout.Label(itemInfo.Template.ID, GUILayout.Width(160f));

                GUILayout.BeginVertical();
                for (int i = 0; i < itemInfo.Infos.Count; i++)
                {
                    AssetInfo info = itemInfo.Infos[i];
                    if (_hideAvailable && info.Asset != null) continue;
                    if (_hideOptional && info.IsOptional) continue;

                    GUILayout.BeginHorizontal();

                    GUI.backgroundColor = info.IsOptional ?
                        info.Asset == null ? Color.yellow : Color.cyan :
                        info.Asset == null ? Color.red : Color.green;
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(info.Asset, info.AssetType, false, GUILayout.Width(140f));
                    GUI.enabled = true;
                    GUI.backgroundColor = guiBackgroundColor;

                    GUILayout.Label(info.Key);

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.Space(3.0f);
            }

            GUILayout.EndScrollView();
        }

        private void CheckAssets()
        {
            ItemInfos.Clear();

            List<AddressableAssetEntry> addressableAssets = new();
            AddressableAssetSettingsDefaultObject.Settings.GetAllAssets(addressableAssets, true);
            Dictionary<string, AddressableAssetEntry> addressableAssetsLookup = addressableAssets
                .ToDictionary(assetEntry => assetEntry.address);

            foreach (Template template in ConfigEditorUtility.GetTemplatesAll())
            {
                CheckAssetsForWrapper(template, addressableAssetsLookup);
            }
        }

        private void CheckAssetsForWrapper(Template template,
            Dictionary<string, AddressableAssetEntry> addressableAssetsLookup)
        {
            ItemAssetInfo itemInfo = new ItemAssetInfo();
            itemInfo.Template = template;

            foreach (TemplateAssetAttribute assetAttribute in template.GetType()
                .GetCustomAttributes<TemplateAssetAttribute>())
            {
                AssetInfo info = new AssetInfo();

                string key = $"{template.ID}:{assetAttribute.AssetKey}";
                info.Key = key;
                info.AssetType = assetAttribute.AssetType;
                info.IsOptional = assetAttribute.IsOptional;

                if (addressableAssetsLookup.TryGetValue(key, out AddressableAssetEntry assetEntry) &&
                    assetEntry.MainAssetType == assetAttribute.AssetType)
                {
                    info.Asset = assetEntry.MainAsset;
                }

                if (info.IsOptional)
                {
                    if (info.Asset == null)
                    {
                        itemInfo.Infos.Insert(itemInfo.CountTotalRequired + itemInfo.CountMissingOptional, info);
                        itemInfo.CountMissingOptional++;
                        itemInfo.CountTotalOptional++;
                    }
                    else
                    {
                        itemInfo.Infos.Insert(itemInfo.CountTotalRequired + itemInfo.CountTotalOptional, info);
                        itemInfo.CountTotalOptional++;
                    }
                }
                else
                {
                    if (info.Asset == null)
                    {
                        itemInfo.Infos.Insert(itemInfo.CountMissingRequired, info);
                        itemInfo.CountMissingRequired++;
                        itemInfo.CountTotalRequired++;
                    }
                    else
                    {
                        itemInfo.Infos.Insert(itemInfo.CountTotalRequired, info);
                        itemInfo.CountTotalRequired++;
                    }
                }
            }

            ItemInfos.Add(itemInfo);
        }
    }
}
