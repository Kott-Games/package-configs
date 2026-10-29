using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Writes the payload into a Unity Remote Config file (<c>.rc</c>) under the key of this project version
    ///     (<see cref="ConfigVersionUtility.GetTemplateDataKey" />, the key the <see cref="RemoteConfigLoader" /> reads),
    ///     keeping the file's other entries, and optionally deploys the file.
    /// </summary>
    [Serializable]
    public class RemoteConfigPublishTarget : ConfigPublishTarget
    {
        /// <summary>JSON schema of Remote Config files.</summary>
        public const string Schema = "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json";

        [SerializeField]
        [Tooltip("Asset path of the .rc file. Empty = <set name>.rc next to the config set.")]
        private string _path;

        [SerializeField]
        [Tooltip("Remote Config key. Empty = the key of this project version.")]
        private string _key;

        [SerializeField]
        [Tooltip("Deploy the file to the current environment after writing it.")]
        private bool _deploy;

        /// <summary>Asset path of the <c>.rc</c> file. Empty = <c>&lt;set name&gt;.rc</c> next to the config set.</summary>
        public string Path
        {
            get => _path;
            set => _path = value;
        }

        /// <summary>Remote Config key; <c>null</c> or empty = the key of this project version.</summary>
        public string Key
        {
            get => _key;
            set => _key = value;
        }

        /// <summary>Whether to deploy the file to the current environment after writing it.</summary>
        public bool Deploy
        {
            get => _deploy;
            set => _deploy = value;
        }

        /// <summary>The key written: <see cref="Key" />, or the key of this project version.</summary>
        public string EffectiveKey => string.IsNullOrEmpty(_key) ? ConfigVersionUtility.GetTemplateDataKey() : _key;

        /// <inheritdoc />
        public override string Name => $"{nameof(RemoteConfigPublishTarget)} '{EffectiveKey}'";

        /// <summary>The path written for <paramref name="set" />.</summary>
        public string GetPath(ConfigSet set)
        {
            return string.IsNullOrEmpty(_path)
                ? ConfigPipeline.GetDefaultOutputPath(set, ".rc")
                : _path.Replace('\\', '/');
        }

        /// <inheritdoc />
        public override string[] GetOutputPaths(ConfigSet set) => new[] { GetPath(set) };

        /// <inheritdoc />
        public override void Publish(ConfigPublishContext context)
        {
            string path = GetPath(context.Set);
            FilePublishTarget.WriteFile(path, Write(File.Exists(path) ? File.ReadAllText(path) : null,
                EffectiveKey, context.Payload));

            if (_deploy) DeployAsync(path);
        }

        /// <summary>
        ///     The <c>.rc</c> file <paramref name="existing" /> (or a new one) with <paramref name="payload" /> under
        ///     <paramref name="key" />; other entries are kept.
        /// </summary>
        /// <exception cref="ConfigException"><paramref name="existing" /> is no valid Remote Config file.</exception>
        public static string Write(string existing, string key, JObject payload)
        {
            JObject file;
            try
            {
                file = string.IsNullOrWhiteSpace(existing) ? new JObject() : JObject.Parse(existing);
            }
            catch (Exception exception) when (exception is Newtonsoft.Json.JsonException)
            {
                throw new ConfigException($"The Remote Config file is not valid JSON: {exception.Message}", exception);
            }

            file["$schema"] ??= Schema;
            if (file["entries"] is not JObject entries)
            {
                if (file["entries"] != null) throw new ConfigException("The Remote Config file's 'entries' is no object.");
                entries = new JObject();
                file["entries"] = entries;
            }

            entries[key] = payload.DeepClone();
            return file.ToString();
        }

        private static async void DeployAsync(string path)
        {
            try
            {
                DeploymentResult<IDeploymentItem> result = await DeployUtility.DeployAsync(new[] { path });
                if (result.Deployed.Count == 0)
                {
                    Debug.LogError($"[Configs] Deploying '{path}' failed, see the Deployment window.");
                }
                else
                {
                    Debug.Log($"[Configs] Deployed '{path}'.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
