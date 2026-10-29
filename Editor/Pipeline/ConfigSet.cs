using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     A set of templates that is validated, baked and published together: all template assets and JSON template
    ///     files (flat arrays of templates) in its folders. A template belongs to exactly one set, IDs are unique within
    ///     the set and references stay inside it. Push with <see cref="ConfigPipeline.Push" />.
    /// </summary>
    [CreateAssetMenu(menuName = "Kott/Configs/Config Set", fileName = "ConfigSet", order = 0)]
    public class ConfigSet : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Folders (with subfolders) holding the templates. Empty = the folder of this asset.")]
        private List<DefaultAsset> _folders = new();

        [SerializeField]
        [Tooltip("Type mapping written on push; reference it from the runtime (e.g. the ConfigManager). Created " +
                 "next to this asset if empty.")]
        private ConfigTypeMapping _typeMapping;

        [SerializeReference]
        [SubtypePicker]
        [Tooltip("Where the baked payload goes on push.")]
        private List<ConfigPublishTarget> _publishTargets = new();

        /// <summary>Folders (with subfolders) holding the templates. Empty = the folder of this asset.</summary>
        public List<DefaultAsset> Folders => _folders;

        /// <summary>Type mapping written on push. Created next to this asset by the first push if <c>null</c>.</summary>
        public ConfigTypeMapping TypeMapping
        {
            get => _typeMapping;
            set => _typeMapping = value;
        }

        /// <summary>Where the baked payload goes on push.</summary>
        public List<ConfigPublishTarget> PublishTargets => _publishTargets;

        /// <summary>The asset paths of <see cref="Folders" />, or the folder of this asset if there are none.</summary>
        public List<string> GetFolderPaths()
        {
            List<string> paths = new();
            foreach (DefaultAsset folder in _folders)
            {
                string path = folder ? AssetDatabase.GetAssetPath(folder) : null;
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path)) paths.Add(path);
            }

            if (_folders.Count == 0 && GetDirectory() is { } directory) paths.Add(directory);
            return paths;
        }

        /// <summary>The folder of this asset (asset path), or <c>null</c> if it is not saved.</summary>
        public string GetDirectory()
        {
            string path = AssetDatabase.GetAssetPath(this);
            return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path)?.Replace('\\', '/');
        }
    }
}
