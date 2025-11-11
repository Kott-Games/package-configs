using System;
using System.Collections.Generic;

namespace Kott.Configs
{
    [Serializable]
    public class TemplateDescription
    {
        public string GroupID;
        public List<string> ParentGroupIDs = new();

        public Type TemplateType;
        public string TemplateTypeString;
    }
}
