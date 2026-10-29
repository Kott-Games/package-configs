using System;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Editor only: bakes a config set from its current templates when loading, so changes can be tried in play
    ///     mode without publishing. Put it last, so it overrides the other loaders. Loads nothing in a player build.
    /// </summary>
    [Serializable]
    public class EditorLoader : ConfigLoader
    {
#if UNITY_EDITOR
        [SerializeField]
        [Tooltip("The config set (Kott.Configs.ConfigSet) to bake in the editor.")]
        private ScriptableObject _configSet;

        /// <summary>The config set to bake. Its type is in the editor assembly, hence <see cref="ScriptableObject" />.</summary>
        public ScriptableObject ConfigSet
        {
            get => _configSet;
            set => _configSet = value;
        }

        /// <summary>Bakes a config set to payload JSON; registered by the editor assembly.</summary>
        internal static Func<ScriptableObject, string> BakeHandler { get; set; }

        /// <inheritdoc />
        public override string Name =>
            _configSet ? $"{nameof(EditorLoader)} '{_configSet.name}'" : nameof(EditorLoader);
#endif

        /// <inheritdoc />
        public override Task<string> LoadAsync(IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
#if UNITY_EDITOR
            if (!_configSet) throw new ConfigException("No config set assigned.");
            if (BakeHandler == null) throw new ConfigException("The Kott.Configs.Editor assembly is not loaded.");

            string json = BakeHandler(_configSet);
            progress?.Report(new ProgressReport(1f, null));
            return Task.FromResult(json);
#else
            return Task.FromResult<string>(null);
#endif
        }
    }
}
