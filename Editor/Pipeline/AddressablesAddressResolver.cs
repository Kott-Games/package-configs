using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>Addresses from the project's Addressables settings; sub-assets as <c>"address[name]"</c>.</summary>
    internal sealed class AddressablesAddressResolver : IAssetAddressResolver
    {
        public string GetAddress(Object asset)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _)) return null;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings ? settings.FindAssetEntry(guid, true) : null;
            if (entry == null) return null;

            return AssetDatabase.IsMainAsset(asset) ? entry.address : $"{entry.address}[{asset.name}]";
        }

        public void MakeAddressable(Object asset)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _)) return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        }
    }
}
