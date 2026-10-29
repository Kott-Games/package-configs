using System.Collections.Generic;
using System.Linq;
using Kott.Configs.Testing;
using NUnit.Framework;

namespace Kott.Configs.Basics.Tests
{
    /// <summary>Game-side unit tests: templates created in code with <see cref="InMemoryConfigProvider" />, no assets.</summary>
    public class BestiaryTests
    {
        private InMemoryConfigProvider _configs;

        [SetUp]
        public void SetUp()
        {
            _configs = new InMemoryConfigProvider();
            AddElement(Element.Fire, Element.Ice);
            AddElement(Element.Ice, Element.Fire);
            AddElement(Element.Poison, Element.Fire);
        }

        [TearDown]
        public void TearDown()
        {
            _configs.Dispose();
        }

        [Test]
        public void Build_EnemiesAndBosses_RegularFirstThenByThreat()
        {
            AddEnemy("Enemy+Wolf", 20, 3);
            AddEnemy("Enemy+Rat", 5, 1);
            _configs.Add<BossTemplate>("Enemy+Dragon", boss => (boss.Health, boss.Damage) = (1, 1));

            List<BestiaryEntry> entries = Bestiary.Build(_configs);

            Assert.That(entries.Select(entry => entry.Enemy.ID),
                Is.EqualTo(new[] { "Enemy+Rat", "Enemy+Wolf", "Enemy+Dragon" }));
        }

        [Test]
        public void Build_Element_UsesEnumMappedTemplates()
        {
            AddEnemy("Enemy+Imp", 1, 1, Element.Poison);

            BestiaryEntry entry = Bestiary.Build(_configs).Single();

            Assert.That(entry.Element.ID, Is.EqualTo("Element+Poison"));
            Assert.That(entry.Weakness.ID, Is.EqualTo("Element+Fire"));
        }

        [Test]
        public void Build_ElementWithoutTemplate_Throws()
        {
            _configs.Dispose();
            _configs = new InMemoryConfigProvider();
            AddElement(Element.Fire, Element.Ice);
            AddEnemy("Enemy+Imp", 1, 1);

            Assert.Throws<KeyNotFoundException>(() => Bestiary.Build(_configs));
        }

        [Test]
        public void Threat_Boss_MultipliesPhases()
        {
            _configs.Add<BossTemplate>("Enemy+Dragon", boss => (boss.Health, boss.Damage, boss.Phases) = (10, 2, 3));

            Assert.That(Bestiary.Build(_configs).Single().Threat, Is.EqualTo(60));
        }

        [Test]
        public void Loot_EmptyReference_IsSkipped()
        {
            ItemTemplate coin = _configs.Add<ItemTemplate>("Item+Coin", item => item.Value = 2);
            ItemTemplate fang = _configs.Add<ItemTemplate>("Item+Fang", item => item.Value = 5);
            EnemyTemplate wolf = AddEnemy("Enemy+Wolf", 1, 1);
            wolf.Loot.Add(Reference(coin));
            wolf.Loot.Add(new TemplateReference<ItemTemplate>());
            wolf.Loot.Add(Reference(fang));

            BestiaryEntry entry = Bestiary.Build(_configs).Single();

            Assert.That(entry.Loot, Is.EqualTo(new[] { coin, fang }));
            Assert.That(entry.LootValue, Is.EqualTo(7));
        }

        private void AddElement(Element element, Element weakness)
        {
            _configs.Add<ElementTemplate>(TemplateIdUtility.CreateEnumId(typeof(ElementTemplate), element), template =>
            {
                template.DisplayName = element.ToString();
                template.Weakness = weakness;
            });
        }

        private EnemyTemplate AddEnemy(string id, int health, int damage, Element element = Element.Ice)
        {
            return _configs.Add<EnemyTemplate>(id, enemy =>
            {
                enemy.Health = health;
                enemy.Damage = damage;
                enemy.Element = element;
            });
        }

        private static TemplateReference<T> Reference<T>(T template) where T : Template
        {
            TemplateReference<T> reference = new();
            ((ITemplateReference)reference).Template = template;
            return reference;
        }
    }
}
