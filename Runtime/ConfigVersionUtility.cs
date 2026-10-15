using System;
using Kott.Utilities;

namespace Kott.Configs
{
    public static class ConfigVersionUtility
    {
        public const string TemplateDataKeyBase = "configTemplates";

        public static string GetTemplateDataKey()
        {
            Version version = VersionUtility.GetAppVersion();
            string versionSuffix = VersionUtility.GetPseudoRandomVersionString(version);

            return TemplateDataKeyBase + versionSuffix;
        }
    }
}
