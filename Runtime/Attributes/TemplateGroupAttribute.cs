using System;

namespace Kott.Configs.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class TemplateGroupAttribute : Attribute
    {
        public TemplateGroupAttribute(string groupID)
        {
            GroupID = groupID;
        }

        public string GroupID { get; }
        public string DisplayName { get; set; }
    }
}
