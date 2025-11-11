using System;
using System.Collections.Generic;

namespace ToG.Config
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
