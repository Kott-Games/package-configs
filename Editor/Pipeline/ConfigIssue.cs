using System;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>How bad a <see cref="ConfigIssue" /> is.</summary>
    public enum ConfigIssueSeverity
    {
        /// <summary>Blocks publishing.</summary>
        Error,

        /// <summary>Publishing works, but the data is probably not what was meant.</summary>
        Warning,
    }

    /// <summary>A problem found while building a <see cref="ConfigSet" />.</summary>
    public sealed class ConfigIssue
    {
        /// <param name="severity">How bad it is.</param>
        /// <param name="message">What is wrong, naming the template.</param>
        /// <param name="context">The asset to select: template, JSON file or config set.</param>
        /// <param name="line">Line in a JSON file, 0 = none.</param>
        /// <param name="fix">Fixes the issue automatically, if possible.</param>
        public ConfigIssue(ConfigIssueSeverity severity, string message, UnityEngine.Object context = null,
            int line = 0, Action fix = null)
        {
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Context = context;
            Line = line;
            Fix = fix;
        }

        /// <summary>How bad it is.</summary>
        public ConfigIssueSeverity Severity { get; }

        /// <summary>What is wrong, naming the template.</summary>
        public string Message { get; }

        /// <summary>The asset to select: template, JSON file or config set. May be <c>null</c>.</summary>
        public UnityEngine.Object Context { get; }

        /// <summary>Line in a JSON file, 0 = none.</summary>
        public int Line { get; }

        /// <summary>Fixes the issue automatically; <c>null</c> if that's not possible.</summary>
        public Action Fix { get; }

        /// <summary>Logs the issue with its context, as error or warning.</summary>
        public void Log(ILogger logger = null)
        {
            LogType type = Severity == ConfigIssueSeverity.Error ? LogType.Error : LogType.Warning;
            (logger ?? Debug.unityLogger).Log(type, (object)ToString(), Context);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Line > 0 ? $"[Configs] {Message} (line {Line})" : $"[Configs] {Message}";
        }
    }
}
