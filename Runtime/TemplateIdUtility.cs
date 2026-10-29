using System;
using System.Reflection;
using Kott.Configs.Attributes;

namespace Kott.Configs
{
    /// <summary>Derives template IDs and type identifiers, the same way the editor does when publishing.</summary>
    public static class TemplateIdUtility
    {
        /// <summary>Separator between group ID and name in a template ID.</summary>
        public const char Separator = '+';

        /// <summary>
        ///     The group ID of a template type: <see cref="TemplateGroupAttribute.GroupID" /> (inherited) or the type name.
        /// </summary>
        public static string GetGroupId(Type templateType)
        {
            if (templateType == null) throw new ArgumentNullException(nameof(templateType));
            return templateType.GetCustomAttribute<TemplateGroupAttribute>(true)?.GroupID ?? templateType.Name;
        }

        /// <summary>The ID of a template asset of <paramref name="templateType" /> named <paramref name="name" />.</summary>
        public static string CreateId(Type templateType, string name)
        {
            return GetGroupId(templateType) + Separator + name;
        }

        /// <summary>
        ///     The ID of the template that <paramref name="value" /> maps to for <paramref name="templateType" />:
        ///     <see cref="TemplateEnumAttribute.IdFormat" />, or <c>"&lt;group ID&gt;+&lt;value name&gt;"</c>.
        /// </summary>
        public static string CreateEnumId(Type templateType, Enum value)
        {
            if (templateType == null) throw new ArgumentNullException(nameof(templateType));
            if (value == null) throw new ArgumentNullException(nameof(value));

            string format = templateType.GetCustomAttribute<TemplateEnumAttribute>(false)?.IdFormat
                            ?? GetGroupId(templateType) + Separator + "{0}";
            return string.Format(format, value);
        }

        /// <summary>
        ///     The identifier of a template type in payloads: <see cref="TemplateTypeAttribute.Identifier" />, or the
        ///     class name without "Template".
        /// </summary>
        public static string GetTypeIdentifier(Type templateType)
        {
            if (templateType == null) throw new ArgumentNullException(nameof(templateType));
            return templateType.GetCustomAttribute<TemplateTypeAttribute>(false)?.Identifier
                   ?? templateType.Name.Replace("Template", null);
        }
    }
}
