using System;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>What a <see cref="ConfigProvider" /> does when a loader fails.</summary>
    public enum ConfigLoaderPolicy
    {
        /// <summary>The failure fails the whole load.</summary>
        Required,

        /// <summary>The failure is logged as a warning; the load continues with the other loaders.</summary>
        Optional,
    }

    /// <summary>
    ///     Fetches one config payload (see <see cref="ConfigSerialization" />) for a <see cref="ConfigProvider" />.
    ///     Implement it for your own sources; subclasses must be <c>[Serializable]</c> to be set up in the inspector.
    /// </summary>
    [Serializable]
    public abstract class ConfigLoader
    {
        [SerializeField]
        private ConfigLoaderPolicy _policy = ConfigLoaderPolicy.Required;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds until the load fails. 0 = no timeout.")]
        private float _timeout;

        /// <summary>What happens when this loader fails. Default: <see cref="ConfigLoaderPolicy.Required" />.</summary>
        public ConfigLoaderPolicy Policy
        {
            get => _policy;
            set => _policy = value;
        }

        /// <summary>Seconds until the load fails (and is cancelled). 0 = no timeout.</summary>
        public float Timeout
        {
            get => _timeout;
            set => _timeout = Mathf.Max(0f, value);
        }

        /// <summary>Name of the loader in log messages and errors.</summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        ///     Fetches the payload JSON. Throw (or return a faulted task) on failure; return <c>null</c> when there is
        ///     deliberately nothing to load (e.g. an optional override file that doesn't exist).
        /// </summary>
        /// <param name="progress">Progress of this load (0–1). May be <c>null</c>.</param>
        /// <param name="cancellationToken">Cancelled on timeout or when the provider's load is cancelled.</param>
        public abstract Task<string> LoadAsync(IProgress<ProgressReport> progress, CancellationToken cancellationToken);
    }
}
