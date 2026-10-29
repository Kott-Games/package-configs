using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kott.Scopes;
using Kott.Utilities.Progress;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Kott.Configs
{
    /// <summary>
    ///     Scopes manager providing <see cref="IConfigManager" />: loads configs with the loaders set up in the inspector
    ///     (see <see cref="ConfigProvider" />) while its scope initializes, and destroys them when it unloads.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Kott.Configs")]
    public class ConfigManager : Manager, IConfigManager
    {
        [SerializeField]
        [FormerlySerializedAs("_configTypeMapping")]
        private ConfigTypeMapping _typeMapping;

        [SerializeReference]
        [SubtypePicker]
        [Tooltip("Lowest priority first: later loaders override templates of earlier ones by ID.")]
        private List<ConfigLoader> _loaders = new();

        [SerializeField]
        [Tooltip("Log loaders, template counts and overrides.")]
        private bool _verbose;

        private ConfigProvider _provider;

        /// <summary>Mapping of the payloads' type identifiers. Set before the scope loads.</summary>
        public ConfigTypeMapping TypeMapping
        {
            get => _typeMapping;
            set => _typeMapping = value;
        }

        /// <summary>The loaders, lowest priority first. Change them before the scope loads.</summary>
        public List<ConfigLoader> Loaders => _loaders;

        /// <summary>The provider while the scope is loaded, e.g. to reload with <see cref="ConfigProvider.LoadAsync" />.</summary>
        public ConfigProvider Provider => _provider;

        /// <inheritdoc />
        public override IEnumerator Initialize(IProgress<ProgressReport> progress)
        {
            _provider = new ConfigProvider(_typeMapping, _loaders, Logger) { Verbose = _verbose };
            Task load = _provider.LoadAsync(progress);
            yield return new WaitForTask(load);

            // rethrows a failure, which fails the scope load
            load.GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override IEnumerator Uninitialize(IProgress<ProgressReport> progress)
        {
            _provider?.Dispose();
            _provider = null;
            yield break;
        }

        /// <inheritdoc />
        public T GetTemplate<T>(string id) where T : Template => LoadedProvider.GetTemplate<T>(id);

        /// <inheritdoc />
        public bool TryGetTemplate<T>(string id, out T template) where T : Template =>
            LoadedProvider.TryGetTemplate(id, out template);

        /// <inheritdoc />
        public T GetTemplate<T>(Enum value) where T : Template => LoadedProvider.GetTemplate<T>(value);

        /// <inheritdoc />
        public IReadOnlyList<T> GetCollection<T>() where T : Template => LoadedProvider.GetCollection<T>();

        private ConfigProvider LoadedProvider =>
            _provider ?? throw new InvalidOperationException($"{nameof(ConfigManager)} is not initialized.");
    }
}
