using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs
{
    public class ConfigTypeMapping : ScriptableObject
    {
        [Serializable]
        public class Group
        {
            [SerializeField] private string _typeIdentifier;
            [SerializeField] private string _typeString;
            [SerializeField] private List<string> _baseTypeStrings = new();
            
            public string TypeIdentifier { get => _typeIdentifier; set => _typeIdentifier = value; }
            public string TypeString { get => _typeString; set => _typeString = value; }
            public List<string> BaseTypeStrings => _baseTypeStrings;
        }
        
        [SerializeField] private List<Group> _typeGroups = new();
        
        public List<Group> TypeGroups => _typeGroups;
    }
}