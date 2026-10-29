using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Writes the payload to a JSON file. Inside <c>Assets/</c> it becomes a <see cref="TextAsset" /> for the
    ///     <see cref="TextAssetLoader" />; in <c>StreamingAssets</c> it is read by the <see cref="FileLoader" />;
    ///     anywhere else an external upload can pick it up.
    /// </summary>
    [Serializable]
    public class FilePublishTarget : ConfigPublishTarget
    {
        [SerializeField]
        [Tooltip("Path relative to the project folder, or absolute. Empty = <set name>.json next to the config set.")]
        private string _path;

        /// <summary>Creates a target writing <c>&lt;set name&gt;.json</c> next to the config set.</summary>
        public FilePublishTarget()
        { }

        /// <summary>Creates a target writing to <paramref name="path" /> (relative to the project folder, or absolute).</summary>
        public FilePublishTarget(string path)
        {
            _path = path;
        }

        /// <summary>
        ///     Path relative to the project folder, or absolute. Empty = <c>&lt;set name&gt;.json</c> next to the set.
        /// </summary>
        public string Path
        {
            get => _path;
            set => _path = value;
        }

        /// <inheritdoc />
        public override string Name =>
            string.IsNullOrEmpty(_path) ? nameof(FilePublishTarget) : $"{nameof(FilePublishTarget)} '{_path}'";

        /// <summary>The path written for <paramref name="set" />.</summary>
        public string GetPath(ConfigSet set)
        {
            return string.IsNullOrEmpty(_path)
                ? ConfigPipeline.GetDefaultOutputPath(set, ".json")
                : _path.Replace('\\', '/');
        }

        /// <inheritdoc />
        public override string[] GetOutputPaths(ConfigSet set) => new[] { GetPath(set) };

        /// <inheritdoc />
        public override void Publish(ConfigPublishContext context)
        {
            WriteFile(GetPath(context.Set), context.Json);
        }

        /// <summary>
        ///     Writes <paramref name="text" /> (UTF-8, no BOM) and imports it if it is inside the project's assets.
        /// </summary>
        public static void WriteFile(string path, string text)
        {
            string directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, text, new UTF8Encoding(false));

            if (path.StartsWith("Assets/", StringComparison.Ordinal) ||
                path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                AssetDatabase.ImportAsset(path);
            }
        }
    }
}
