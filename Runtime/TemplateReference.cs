using System;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     A reference from one template to another, as a template field. Asset templates reference other asset
    ///     templates by object reference; payloads and JSON template files store the target's ID, which the provider
    ///     resolves after loading.
    /// </summary>
    /// <typeparam name="TTemplate">The template type the reference accepts (subtypes included).</typeparam>
    [Serializable]
    public class TemplateReference<TTemplate> : ITemplateReference
        where TTemplate : Template
    {
        [SerializeField]
        private TTemplate _template;

        Template ITemplateReference.Template
        {
            get => _template;
            set => _template = value as TTemplate;
        }

        /// <summary>The referenced template, or <c>null</c> if the reference is empty or didn't resolve.</summary>
        public TTemplate Template => _template;

        /// <summary>The referenced template; <c>null</c> for an empty or <c>null</c> reference.</summary>
        public static implicit operator TTemplate(TemplateReference<TTemplate> reference) => reference?.Template;
    }
}
