using System;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Loads the payload from Unity Remote Config, under the key of this build's version
    ///     (<see cref="ConfigVersionUtility.GetTemplateDataKey" />), so each build reads its own config branch.
    /// </summary>
    [Serializable]
    public class RemoteConfigLoader : ConfigLoader
    {
        [SerializeField]
        [Tooltip("Remote Config key. Empty = the key of this build's version.")]
        private string _key;

        [SerializeField]
        [Tooltip("Initialize Unity Services and sign in anonymously if not done yet.")]
        private bool _initializeServices = true;

        /// <summary>Remote Config key; <c>null</c> or empty = the key of this build's version.</summary>
        public string Key
        {
            get => _key;
            set => _key = value;
        }

        /// <summary>Whether to initialize Unity Services and sign in anonymously if not done yet. Default: on.</summary>
        public bool InitializeServices
        {
            get => _initializeServices;
            set => _initializeServices = value;
        }

        /// <summary>The key read: <see cref="Key" />, or the key of this build's version.</summary>
        public string EffectiveKey => string.IsNullOrEmpty(_key) ? ConfigVersionUtility.GetTemplateDataKey() : _key;

        /// <inheritdoc />
        public override string Name => $"{nameof(RemoteConfigLoader)} '{EffectiveKey}'";

        /// <inheritdoc />
        public override async Task<string> LoadAsync(IProgress<ProgressReport> progress,
            CancellationToken cancellationToken)
        {
            string json = await FetchJsonAsync(EffectiveKey, cancellationToken);
            progress?.Report(new ProgressReport(1f, null));
            return json;
        }

        /// <summary>Fetches the JSON value of <paramref name="key" />. Overridden by tests.</summary>
        internal virtual async Task<string> FetchJsonAsync(string key, CancellationToken cancellationToken)
        {
            if (_initializeServices)
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            RuntimeConfig config = await RemoteConfigService.Instance.FetchConfigsAsync(
                new NoAttributes(), new NoAttributes(), new KeyFilter { key = new[] { key } });
            cancellationToken.ThrowIfCancellationRequested();

            if (!config.HasKey(key))
            {
                throw new ConfigException($"Remote Config has no key '{key}' (origin: {config.origin}).");
            }

            return config.GetJson(key, null);
        }

        private struct NoAttributes
        { }

        // field name is the filter attribute name of the Remote Config API
        private struct KeyFilter
        {
            // ReSharper disable once InconsistentNaming
            public string[] key;
        }
    }
}
