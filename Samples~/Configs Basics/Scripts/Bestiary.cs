using System;
using System.Collections.Generic;
using System.Linq;

namespace Kott.Configs.Basics
{
    /// <summary>What the bestiary shows about one enemy: its template plus data looked up from other templates.</summary>
    public sealed class BestiaryEntry
    {
        /// <param name="enemy">The enemy (or boss) template.</param>
        /// <param name="element">The template of the enemy's element.</param>
        /// <param name="weakness">The template of the element's weakness.</param>
        public BestiaryEntry(EnemyTemplate enemy, ElementTemplate element, ElementTemplate weakness)
        {
            Enemy = enemy;
            Element = element;
            Weakness = weakness;
        }

        /// <summary>The enemy (or boss) template.</summary>
        public EnemyTemplate Enemy { get; }

        /// <summary>The template of the enemy's element.</summary>
        public ElementTemplate Element { get; }

        /// <summary>The template of the element's weakness.</summary>
        public ElementTemplate Weakness { get; }

        /// <summary>The enemy as boss, or <c>null</c>.</summary>
        public BossTemplate Boss => Enemy as BossTemplate;

        /// <summary>How dangerous the enemy is: health × damage, × phases for bosses.</summary>
        public int Threat => Enemy.Health * Enemy.Damage * (Boss ? Boss.Phases : 1);

        /// <summary>The dropped items. References that are empty (or didn't resolve) are skipped.</summary>
        public IEnumerable<ItemTemplate> Loot =>
            Enemy.Loot.Select(reference => reference.Template).Where(item => item);

        /// <summary>Total value of the dropped items.</summary>
        public int LootValue => Loot.Sum(item => item.Value);
    }

    /// <summary>Builds the bestiary from loaded configs. Plain C#: works with any <see cref="IConfigProvider" />.</summary>
    public static class Bestiary
    {
        /// <summary>All enemies including bosses: regular enemies first, then by threat and ID.</summary>
        /// <exception cref="KeyNotFoundException">An element has no template.</exception>
        public static List<BestiaryEntry> Build(IConfigProvider configs)
        {
            if (configs == null) throw new ArgumentNullException(nameof(configs));

            // the base type's collection includes all subtypes: enemies and bosses
            return configs.GetCollection<EnemyTemplate>()
                .Select(enemy =>
                {
                    // enum mapping: Element.Fire → the template "Element+Fire"
                    ElementTemplate element = configs.GetTemplate<ElementTemplate>(enemy.Element);
                    return new BestiaryEntry(enemy, element, configs.GetTemplate<ElementTemplate>(element.Weakness));
                })
                .OrderBy(entry => entry.Enemy is BossTemplate)
                .ThenBy(entry => entry.Threat)
                .ThenBy(entry => entry.Enemy.ID, StringComparer.Ordinal)
                .ToList();
        }
    }
}
