using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using UnityEngine;
using UnityEngine.Networking;

namespace Kott.Configs
{
    /// <summary>Loads a payload file from <c>StreamingAssets</c>, the persistent data path or an absolute path.</summary>
    [Serializable]
    public class FileLoader : ConfigLoader
    {
        /// <summary>The folder <see cref="FileLoader.Path" /> is relative to.</summary>
        public enum PathRoot
        {
            /// <summary><see cref="Application.streamingAssetsPath" />.</summary>
            StreamingAssets,

            /// <summary><see cref="Application.persistentDataPath" />.</summary>
            PersistentData,

            /// <summary><see cref="FileLoader.Path" /> is absolute (or a URL).</summary>
            Absolute,
        }

        [SerializeField]
        private PathRoot _root = PathRoot.StreamingAssets;

        [SerializeField]
        private string _path = "config.json";

        [SerializeField]
        [Tooltip("A missing file loads nothing, without warning (e.g. an optional override file).")]
        private bool _skipIfMissing;

        /// <summary>Creates a loader for <c>StreamingAssets/config.json</c> (change it in the inspector).</summary>
        public FileLoader()
        { }

        /// <summary>Creates a loader for <paramref name="path" /> relative to <paramref name="root" />.</summary>
        public FileLoader(PathRoot root, string path, bool skipIfMissing = false)
        {
            _root = root;
            _path = path;
            _skipIfMissing = skipIfMissing;
        }

        /// <summary>The folder <see cref="Path" /> is relative to.</summary>
        public PathRoot Root
        {
            get => _root;
            set => _root = value;
        }

        /// <summary>Path of the payload file, relative to <see cref="Root" />.</summary>
        public string Path
        {
            get => _path;
            set => _path = value;
        }

        /// <summary>Whether a missing file loads nothing, without warning, instead of failing.</summary>
        public bool SkipIfMissing
        {
            get => _skipIfMissing;
            set => _skipIfMissing = value;
        }

        /// <summary>The full path (or URL, e.g. <c>StreamingAssets</c> on Android and WebGL).</summary>
        public string FullPath =>
            _root switch
            {
                PathRoot.StreamingAssets => System.IO.Path.Combine(Application.streamingAssetsPath, _path),
                PathRoot.PersistentData => System.IO.Path.Combine(Application.persistentDataPath, _path),
                _ => _path,
            };

        /// <inheritdoc />
        public override string Name => $"{nameof(FileLoader)} '{_path}'";

        /// <inheritdoc />
        public override async Task<string> LoadAsync(IProgress<ProgressReport> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_path)) throw new ConfigException("No path set.");

            string fullPath = FullPath;
            string text = fullPath.Contains("://")
                ? await ReadUrlAsync(fullPath, cancellationToken)
                : ReadFile(fullPath);

            progress?.Report(new ProgressReport(1f, null));
            return text;
        }

        // synchronous: config files are small, and it keeps the continuation on the calling thread
        private string ReadFile(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                if (_skipIfMissing) return null;
                throw new FileNotFoundException($"Config file not found: {fullPath}", fullPath);
            }

            return File.ReadAllText(fullPath);
        }

        private async Task<string> ReadUrlAsync(string url, CancellationToken cancellationToken)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            TaskCompletionSource<bool> completion = new();
            request.SendWebRequest().completed += _ => completion.TrySetResult(true);

            using (cancellationToken.Register(request.Abort))
            {
                await completion.Task;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (request.result == UnityWebRequest.Result.Success) return request.downloadHandler.text;
            if (request.responseCode == 404 && _skipIfMissing) return null;
            throw new IOException($"Loading config file {url} failed: {request.error}");
        }
    }
}
