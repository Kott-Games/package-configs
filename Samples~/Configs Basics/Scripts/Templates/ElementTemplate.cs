using Kott.Configs.Attributes;
using UnityEngine;

namespace Kott.Configs.Basics
{
    /// <summary>
    ///     Data of an <see cref="Basics.Element" />. Enum mapped: the template asset named like the value ("Element+Fire")
    ///     is found with <c>configs.GetTemplate&lt;ElementTemplate&gt;(Element.Fire)</c>.
    /// </summary>
    [TemplateGroup("Element", DisplayName = "Elements")]
    [TemplateEnum(typeof(Element))]
    [CreateAssetMenu(menuName = "Configs Basics/Element", fileName = "Element", order = 0)]
    public class ElementTemplate : Template
    {
        public string DisplayName;
        public Color Color = Color.white;

        [Tooltip("Element that deals extra damage to enemies of this element.")]
        public Element Weakness;
    }
}
