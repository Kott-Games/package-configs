using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Kott.Configs.Basics.Tests
{
    /// <summary>
    ///     Pipeline tests against a small config set of their own (<c>TestConfigs/</c>): asset templates plus a JSON file,
    ///     so the sample's data can change without breaking the tests.
    /// </summary>
    public class TestSetTests
    {
        private const string SetName = "BasicsTestSet";

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Build_TestSet_HasNoIssues()
        {
            ConfigBuildResult result = ConfigPipeline.Build(FindSet());

            Assert.That(result.Issues.Select(issue => issue.Message), Is.Empty);
            Assert.That(result.TemplateCount, Is.EqualTo(8));
        }

        [Test]
        public void Load_BakedTestSet_ResolvesReferencesAcrossAssetsAndJson()
        {
            using ConfigProvider configs = LoadBaked();

            Dictionary<string, BestiaryEntry> entries = Bestiary.Build(configs).ToDictionary(entry => entry.Enemy.ID);

            // JSON → asset
            Assert.That(entries["Enemy+Rat"].Loot.Select(item => item.ID), Is.EqualTo(new[] { "Item+Coin" }));
            // JSON → JSON
            Assert.That(entries["Enemy+RatKing"].Boss.Minion.Template.ID, Is.EqualTo("Enemy+Rat"));
            // asset → asset
            Assert.That(entries["Enemy+Ogre"].Boss.Minion.Template.ID, Is.EqualTo("Enemy+Goblin"));
            Assert.That(entries["Enemy+Ogre"].LootValue, Is.EqualTo(2));
        }

        [Test]
        public void Load_BakedTestSet_BossesAreEnemies()
        {
            using ConfigProvider configs = LoadBaked();

            Assert.That(configs.GetCollection<EnemyTemplate>().Count, Is.EqualTo(4));
            Assert.That(configs.GetCollection<BossTemplate>().Select(boss => boss.ID),
                Is.EquivalentTo(new[] { "Enemy+Ogre", "Enemy+RatKing" }));
        }

        private static ConfigSet FindSet()
        {
            ConfigSet set = ConfigPipeline.FindSets().SingleOrDefault(candidate => candidate.name == SetName);
            Assert.That(set, Is.Not.Null, $"Config set '{SetName}' not found.");
            return set;
        }

        // bakes the set like a push, then loads the payload like the game
        private ConfigProvider LoadBaked()
        {
            ConfigBuildResult result = ConfigPipeline.Build(FindSet());
            Assert.That(result.HasErrors, Is.False);

            ConfigTypeMapping mapping = ConfigTypeMapping.Create(result.TemplateTypes.ToArray());
            TextAsset payload = new(result.Payload.ToString());
            _created.Add(mapping);
            _created.Add(payload);

            ConfigProvider configs = new(mapping, new ConfigLoader[] { new TextAssetLoader(payload) });
            // completes synchronously: the text asset loader has nothing to wait for
            configs.LoadAsync().GetAwaiter().GetResult();
            return configs;
        }
    }
}
