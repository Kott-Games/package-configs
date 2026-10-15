using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs
{
    public class RuntimeConfigData : ScriptableObject
    {
        [SerializeField] private List<Template> _templates = new();
        
        public List<Template> Templates => _templates;
    }
}