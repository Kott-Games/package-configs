using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>What a <see cref="ConfigPublishTarget" /> publishes.</summary>
    public sealed class ConfigPublishContext
    {
        /// <param name="set">The published set.</param>
        /// <param name="payload">The baked payload.</param>
        public ConfigPublishContext(ConfigSet set, JObject payload)
        {
            Set = set ? set : throw new ArgumentNullException(nameof(set));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Json = payload.ToString();
        }

        /// <summary>The published set.</summary>
        public ConfigSet Set { get; }

        /// <summary>The baked payload (see <see cref="ConfigSerialization" />). Don't modify it.</summary>
        public JObject Payload { get; }

        /// <summary><see cref="Payload" /> as indented JSON.</summary>
        public string Json { get; }
    }

    /// <summary>
    ///     Where a <see cref="ConfigSet" /> publishes its payload on push. Implement it for your own destinations
    ///     (e.g. an upload to your server); subclasses must be <c>[Serializable]</c> to be set up in the inspector.
    /// </summary>
    [Serializable]
    public abstract class ConfigPublishTarget
    {
        [SerializeField]
        private bool _enabled = true;

        /// <summary>Whether pushes publish to this target. Default: on.</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Name of the target in log messages.</summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        ///     Asset paths this target writes for <paramref name="set" />. They are not collected as JSON template files.
        /// </summary>
        public virtual string[] GetOutputPaths(ConfigSet set) => Array.Empty<string>();

        /// <summary>
        ///     Publishes the payload. Throw on failure: the push reports it and continues with the other targets.
        /// </summary>
        public abstract void Publish(ConfigPublishContext context);
    }
}
