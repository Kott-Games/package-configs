using System;
using Kott.Utilities;

namespace Kott.Configs
{
    /// <summary>
    ///     The Remote Config key of the payload, per app version: each build reads its own config branch, so
    ///     publishing data for a new version doesn't change what older builds load.
    /// </summary>
    public static class ConfigVersionUtility
    {
        /// <summary>Prefix of every payload key; the version-dependent suffix follows.</summary>
        public const string TemplateDataKeyBase = "configTemplates";

        /// <summary>The payload key for the current app version (<c>Application.version</c>).</summary>
        public static string GetTemplateDataKey()
        {
            Version version = VersionUtility.GetAppVersion();
            string versionSuffix = VersionUtility.GetPseudoRandomVersionString(version);

            return TemplateDataKeyBase + versionSuffix;
        }
    }
}
