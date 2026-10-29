using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs.Tests
{
    /// <summary>The pipeline on real assets, in a temporary folder.</summary>
    public class ConfigPipelineTests
    {
        private const string Root = "Assets/KottConfigsTests";
        private const string SetFolder = Root + "/Set";

        private RecordingLogHandler _log;

        [SetUp]
        public void SetUp()
        {
            _log = new RecordingLogHandler();
            AssetDatabase.CreateFolder("Assets", "KottConfigsTests");
            AssetDatabase.CreateFolder(Root, "Set");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
            _log.Verify();
        }

        [Test]
        public void Build_SetFolder_CollectsTemplateAssetsAndJsonFiles()
        {
            ConfigSet set = CreateSet(SetFolder, "Set");
            CreateTemplate<TestItemTemplate>(SetFolder, "Sword");
            WriteJson(SetFolder + "/Items.json", $"[{TestPayloads.Item("TestItemTemplate+Arrow")}]");

            ConfigBuildResult result = ConfigPipeline.Build(set);

            Assert.That(result.Issues, Is.Empty, string.Join("\n", result.Issues));
            Assert.That(result.Payload["templates"].Select(entry => (string)entry["ID"]),
                Is.EqualTo(new[] { "TestItemTemplate+Arrow", "TestItemTemplate+Sword" }));
        }

        [Test]
        public void Build_OverlappingFolders_ReportsError()
        {
            ConfigSet outer = CreateSet(Root, "Outer");
            CreateSet(SetFolder, "Inner");

            ConfigBuildResult result = ConfigPipeline.Build(outer);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Issues.First().Message, Does.Contain("overlaps").And.Contain("'Inner'"));
        }

        [Test]
        public void Build_SubAssetTemplate_ReportsError()
        {
            ConfigSet set = CreateSet(SetFolder, "Set");
            TestItemTemplate sword = CreateTemplate<TestItemTemplate>(SetFolder, "Sword");
            TestItemTemplate gem = ScriptableObject.CreateInstance<TestItemTemplate>();
            gem.name = "Gem";
            AssetDatabase.AddObjectToAsset(gem, sword);

            ConfigBuildResult result = ConfigPipeline.Build(set);

            Assert.That(result.Issues.Single().Message, Does.Contain("'Gem' is a sub-asset"));
        }

        [Test]
        public void Push_FilePublishTarget_WritesPayloadForTextAssetLoader()
        {
            ConfigSet set = CreateSet(SetFolder, "Set");
            set.PublishTargets.Add(new FilePublishTarget());
            CreateTemplate<TestItemTemplate>(SetFolder, "Sword").Damage = 7;

            ConfigBuildResult result = ConfigPipeline.Push(set, _log.Logger);

            Assert.That(result.HasErrors, Is.False);
            Assert.That(AssetDatabase.GetAssetPath(set.TypeMapping), Is.EqualTo(SetFolder + "/SetTypeMapping.asset"));
            TextAsset payload = AssetDatabase.LoadAssetAtPath<TextAsset>(SetFolder + "/Set.json");
            using ConfigProvider provider = new(set.TypeMapping, new ConfigLoader[] { new TextAssetLoader(payload) });
            provider.LoadAsync().GetAwaiter().GetResult();
            Assert.That(provider.GetTemplate<TestItemTemplate>("TestItemTemplate+Sword").Damage, Is.EqualTo(7));
        }

        [Test]
        public void Push_Twice_DoesNotCollectItsOwnOutput()
        {
            ConfigSet set = CreateSet(SetFolder, "Set");
            set.PublishTargets.Add(new FilePublishTarget());
            CreateTemplate<TestItemTemplate>(SetFolder, "Sword");

            ConfigPipeline.Push(set, _log.Logger);
            ConfigBuildResult second = ConfigPipeline.Push(set, _log.Logger);

            Assert.That(second.Issues, Is.Empty, string.Join("\n", second.Issues));
        }

        [Test]
        public void Push_WithErrors_PublishesNothing()
        {
            ConfigSet set = CreateSet(SetFolder, "Set");
            set.PublishTargets.Add(new FilePublishTarget());
            WriteJson(SetFolder + "/Broken.json", "[");
            _log.Expect(LogType.Error, "Broken.json: invalid JSON");
            _log.Expect(LogType.Error, "nothing published");
            _log.Expect(LogType.Warning, "has no templates");

            ConfigBuildResult result = ConfigPipeline.Push(set, _log.Logger);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(File.Exists(SetFolder + "/Set.json"), Is.False);
            Assert.That(set.TypeMapping, Is.Null);
        }

        [Test]
        public void EditorLoader_SetWithCurrentTemplates_LoadsThemAndUpdatesMapping()
        {
            ConfigSet set = CreateSet(SetFolder, "Set");
            CreateTemplate<TestWeaponTemplate>(SetFolder, "Bow").Range = 4f;
            ConfigTypeMapping mapping = ScriptableObject.CreateInstance<ConfigTypeMapping>();
            set.TypeMapping = mapping;

            try
            {
                using ConfigProvider provider = new(mapping, new ConfigLoader[] { new EditorLoader { ConfigSet = set } });
                provider.LoadAsync().GetAwaiter().GetResult();

                Assert.That(provider.GetTemplate<TestWeaponTemplate>("TestWeaponTemplate+Bow").Range, Is.EqualTo(4f));
                Assert.That(provider.GetCollection<TestItemTemplate>(), Has.Length.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(mapping);
            }
        }

        private static ConfigSet CreateSet(string folder, string name)
        {
            ConfigSet set = ScriptableObject.CreateInstance<ConfigSet>();
            AssetDatabase.CreateAsset(set, $"{folder}/{name}.asset");
            return set;
        }

        private static T CreateTemplate<T>(string folder, string name) where T : Template
        {
            T template = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(template, $"{folder}/{name}.asset");
            return template;
        }

        private static void WriteJson(string path, string json)
        {
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path);
        }
    }
}
