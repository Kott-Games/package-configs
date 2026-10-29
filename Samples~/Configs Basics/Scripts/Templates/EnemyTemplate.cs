using System.Collections.Generic;
using Kott.Configs.Attributes;
using UnityEngine;

namespace Kott.Configs.Basics
{
    /// <summary>
    ///     An enemy. The group is inherited, so <see cref="BossTemplate" />s get "Enemy+…" IDs too, and
    ///     <c>GetCollection&lt;EnemyTemplate&gt;()</c> returns bosses as well.
    /// </summary>
    [TemplateGroup("Enemy", DisplayName = "Enemies")]
    [CreateAssetMenu(menuName = "Configs Basics/Enemy", fileName = "Enemy", order = 2)]
    public class EnemyTemplate : Template
    {
        public string DisplayName;
        public int Health = 10;
        public int Damage = 1;
        public Element Element;

        [Tooltip("Items dropped on defeat: references to other templates of the set.")]
        public List<TemplateReference<ItemTemplate>> Loot = new();

        [Tooltip("Addressable sprite. The payload holds its address; load it with Icon.LoadAssetAsync.")]
        public TemplateAssetReference<Sprite> Icon = new();
    }
}
