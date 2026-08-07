using System;

namespace Kott.Configs.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TemplateEnumAttribute : Attribute
    {
        public TemplateEnumAttribute(Type enumType)
        {
            EnumType = enumType;
        }

        public Type EnumType { get; }
    }
}
