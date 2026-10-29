using System;

namespace Kott.Configs.Attributes
{
    /// <summary>
    ///     Maps the values of an enum to templates of the attributed type, so they can be looked up with
    ///     <see cref="IConfigProvider.GetTemplate{T}(Enum)" />.
    /// </summary>
    /// <example>
    ///     <code>
    ///     [TemplateEnum(typeof(StatType))]          // StatType.Health → "Stat+Health"
    ///     public class StatTemplate : Template { … }
    ///     </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TemplateEnumAttribute : Attribute
    {
        /// <param name="enumType">The enum whose values name the templates.</param>
        public TemplateEnumAttribute(Type enumType)
        {
            EnumType = enumType;
        }

        /// <summary>The enum whose values name the templates.</summary>
        public Type EnumType { get; }

        /// <summary>
        ///     Format of the template ID for an enum value (<c>{0}</c> = the value's name). Default:
        ///     <c>"&lt;group ID&gt;+{0}"</c>, the ID of a template asset named like the value (see
        ///     <see cref="TemplateIdUtility.GetGroupId" />).
        /// </summary>
        public string IdFormat { get; set; }
    }
}
