using UnityEngine;

namespace Kott.Configs.Basics
{
    /// <summary>A boss: an enemy with several phases that summons minions. Type identifier "Boss".</summary>
    [CreateAssetMenu(menuName = "Configs Basics/Boss", fileName = "Boss", order = 3)]
    public class BossTemplate : EnemyTemplate
    {
        [Min(1)]
        public int Phases = 2;

        public TemplateReference<EnemyTemplate> Minion = new();
    }
}
