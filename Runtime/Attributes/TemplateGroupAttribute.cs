using System;

namespace Kott.Configs.Attributes
{
    /// <summary>
    ///     Groups template types: the group ID prefixes the IDs of the templates (<c>"&lt;group ID&gt;+&lt;asset name&gt;"</c>)
    ///     and the editor lists the types under it. Inherited, so a base class can group all its subtypes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class TemplateGroupAttribute : Attribute
    {
        /// <param name="groupID">The group ID, used as template ID prefix.</param>
        public TemplateGroupAttribute(string groupID)
        {
            GroupID = groupID;
        }

        /// <summary>The group ID, used as template ID prefix.</summary>
        public string GroupID { get; }

        /// <summary>Name of the group in the editor.</summary>
        public string DisplayName { get; set; }
    }
}
