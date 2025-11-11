using System;

namespace ToG.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
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
