using System.Collections.Generic;

namespace Kott.Configs
{
    /// <summary>
    ///     Templates read from one payload by <see cref="ConfigSerialization.Deserialize(ConfigTypeMapping, string)" />.
    ///     Their template references are still empty: they are resolved when the payload is merged by a
    ///     <see cref="ConfigProvider" />.
    /// </summary>
    public sealed class ConfigPayload
    {
        internal ConfigPayload(List<Template> templates, List<PendingReference> references)
        {
            Templates = templates;
            References = references;
        }

        /// <summary>The templates, in payload order. New instances: destroy them when no longer used.</summary>
        public IReadOnlyList<Template> Templates { get; }

        internal IReadOnlyList<PendingReference> References { get; }
    }
}
