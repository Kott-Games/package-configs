using Kott.Configs.Attributes;
using UnityEngine;

namespace Kott.Configs.Basics
{
    /// <summary>An item enemies drop.</summary>
    [TemplateGroup("Item", DisplayName = "Items")]
    [CreateAssetMenu(menuName = "Configs Basics/Item", fileName = "Item", order = 1)]
    public class ItemTemplate : Template
    {
        public string DisplayName;
        public int Value = 1;
    }
}
