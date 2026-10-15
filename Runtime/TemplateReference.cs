using System;
using UnityEngine;

namespace Kott.Configs
{
    [Serializable]
    public class TemplateReference<TTemplate> : ITemplateReference
        where TTemplate : Template
    {
        [SerializeField] private TTemplate _template;

        Template ITemplateReference.Template
        {
            get => _template;
            set => _template = value as TTemplate;
        }

        public TTemplate Template => _template;

        public static implicit operator TTemplate(TemplateReference<TTemplate> reference) => reference.Template;
    }
}
