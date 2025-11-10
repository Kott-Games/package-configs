using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs
{
    public class ConfigStructure : ScriptableObject
    {
        [Serializable]
        public class Group
        {
            public string TypeString;
            public List<string> BaseTypeStrings = new();
            public List<Template> Templates = new();
            public List<Template> MatchingTemplates = new();
        }

        public List<Group> Groups = new();
    }
}
