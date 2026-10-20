using System;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>Loads a payload from a <see cref="TextAsset" /> included in the build, e.g. a baseline config.</summary>
    [Serializable]
    public class TextAssetLoader : ConfigLoader
    {
        [SerializeField]
        private TextAsset _payload;

        /// <summary>Creates a loader without payload (set it in the inspector).</summary>
        public TextAssetLoader()
        { }

        /// <summary>Creates a loader for <paramref name="payload" />.</summary>
        public TextAssetLoader(TextAsset payload)
        {
            _payload = payload;
        }

        /// <summary>The payload asset.</summary>
        public TextAsset Payload
        {
            get => _payload;
            set => _payload = value;
        }

        /// <inheritdoc />
        public override string Name => _payload ? $"{nameof(TextAssetLoader)} '{_payload.name}'" : nameof(TextAssetLoader);

        /// <inheritdoc />
        public override Task<string> LoadAsync(IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
            if (!_payload) throw new ConfigException("No payload text asset assigned.");

            progress?.Report(new ProgressReport(1f, null));
            return Task.FromResult(_payload.text);
        }
    }
}
