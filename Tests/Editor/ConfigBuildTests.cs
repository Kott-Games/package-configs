using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Kott.Configs.Tests
{
    public class ConfigBuildTests
    {
        private readonly List<Object> _objects = new();
        private FakeAddressResolver _addresses;

        [SetUp]
        public void SetUp()
        {
            _addresses = new FakeAddressResolver();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object target in _objects)
            {
                Object.DestroyImmediate(target);
            }

            _objects.Clear();
        }

        [Test]
        public void Run_AssetAndJsonTemplateWithSameData_BakeEqualEntries()
        {
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            sword.Damage = 5;
            JsonTemplateFile file = Json(TestPayloads.Item("TestItemTemplate+Shield", 5));

            ConfigBuildResult result = Run(new[] { sword }, file);

            JObject asset = Entry(result, "TestItemTemplate+Sword");
            JObject json = Entry(result, "TestItemTemplate+Shield");
            asset.Remove("ID");
            json.Remove("ID");
            Assert.That(JToken.DeepEquals(asset, json), Is.True, $"{asset}\n{json}");
        }

        [Test]
        public void Run_AssetTemplate_DerivesIdFromTypeAndName()
        {
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            sword.ID = "TestItemTemplate+OldName";

            ConfigBuildResult result = Run(new[] { sword });

            Assert.That(sword.ID, Is.EqualTo("TestItemTemplate+Sword"));
            Assert.That(Ids(result), Is.EqualTo(new[] { "TestItemTemplate+Sword" }));
        }

        [Test]
        public void Run_JsonReferences_ResolveToAssetAndJsonTemplates()
        {
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            JsonTemplateFile file = Json(
                TestPayloads.Item("TestItemTemplate+A", upgrade: "TestItemTemplate+Sword"),
                TestPayloads.Item("TestItemTemplate+B", upgrade: "TestItemTemplate+A"));

            ConfigBuildResult result = Run(new[] { sword }, file);

            Assert.That(result.Issues, Is.Empty);
            Assert.That(Entry(result, "TestItemTemplate+A")["Upgrade"].Value<string>(),
                Is.EqualTo("TestItemTemplate+Sword"));
            Assert.That(Entry(result, "TestItemTemplate+B")["Upgrade"].Value<string>(),
                Is.EqualTo("TestItemTemplate+A"));
        }

        [Test]
        public void Run_ReferenceCycle_Bakes()
        {
            TestItemTemplate a = Create<TestItemTemplate>("A");
            TestItemTemplate b = Create<TestItemTemplate>("B");
            ((ITemplateReference)a.Upgrade).Template = b;
            ((ITemplateReference)b.Upgrade).Template = a;

            ConfigBuildResult result = Run(new[] { a, b });

            Assert.That(result.Issues, Is.Empty);
            Assert.That(Entry(result, "TestItemTemplate+A")["Upgrade"].Value<string>(), Is.EqualTo("TestItemTemplate+B"));
            Assert.That(Entry(result, "TestItemTemplate+B")["Upgrade"].Value<string>(), Is.EqualTo("TestItemTemplate+A"));
        }

        [Test]
        public void Run_DuplicateId_ReportsBothSources()
        {
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            JsonTemplateFile file = Json(TestPayloads.Item("TestItemTemplate+Sword"));

            ConfigBuildResult result = Run(new[] { sword }, file);

            Assert.That(result.Payload, Is.Null);
            Assert.That(SingleError(result).Message,
                Does.Contain("Duplicate template ID 'TestItemTemplate+Sword'").And.Contain("Sword")
                    .And.Contain(file.Path));
        }

        [Test]
        public void Run_InvalidJson_ReportsFileAndLine()
        {
            JsonTemplateFile file = new("Assets/Broken.json", "[\n  { \"Type\": }\n]");

            ConfigIssue error = SingleError(Run(new Template[0], file));

            Assert.That(error.Message, Does.Contain("Assets/Broken.json").And.Contain("invalid JSON"));
            Assert.That(error.Line, Is.EqualTo(2));
        }

        [Test]
        public void Run_JsonObjectInsteadOfArray_ReportsError()
        {
            JsonTemplateFile file = new("Assets/Payload.json", TestPayloads.Payload(TestPayloads.Item("TestItemTemplate+A")));

            ConfigIssue error = SingleError(Run(new Template[0], file));

            Assert.That(error.Message, Does.Contain("flat array of templates"));
        }

        [Test]
        public void Run_JsonEntryWithoutId_ReportsEntryAndLine()
        {
            JsonTemplateFile file = new("Assets/Items.json",
                "[\n  { \"Type\": \"TestItem\", \"ID\": \"TestItemTemplate+A\" },\n  { \"Type\": \"TestItem\" }\n]");

            ConfigBuildResult result = Run(new Template[0], file);

            ConfigIssue error = SingleError(result);
            Assert.That(error.Message, Does.Contain("entry #1").And.Contain("no 'ID'"));
            Assert.That(error.Line, Is.EqualTo(3));
        }

        [Test]
        public void Run_JsonUnknownField_ReportsError()
        {
            JObject item = TestPayloads.Item("TestItemTemplate+A");
            item["Damgae"] = 3;

            ConfigIssue error = SingleError(Run(new Template[0], Json(item)));

            Assert.That(error.Message, Does.Contain("TestItemTemplate+A").And.Contain("Damgae"));
        }

        [Test]
        public void Run_JsonUnknownType_ReportsError()
        {
            ConfigIssue error = SingleError(Run(new Template[0], Json(TestPayloads.Item("X+A", type: "Missing"))));

            Assert.That(error.Message, Does.Contain("unknown type 'Missing'"));
        }

        [Test]
        public void Run_JsonAmbiguousType_ReportsBothTypes()
        {
            JObject pinned = new() { { "Type", "Pinned" }, { "ID", "Pinned+A" } };

            ConfigIssue error = SingleError(Run(new Template[0], Json(pinned)));

            Assert.That(error.Message,
                Does.Contain(nameof(TestPinnedTemplate)).And.Contain(nameof(TestPinnedTwinTemplate)));
        }

        [Test]
        public void Run_DanglingJsonReference_ReportsError()
        {
            JsonTemplateFile file = Json(TestPayloads.Item("TestItemTemplate+A", upgrade: "TestItemTemplate+Gone"));

            ConfigIssue error = SingleError(Run(new Template[0], file));

            Assert.That(error.Message,
                Does.Contain("TestItemTemplate+A").And.Contain("'TestItemTemplate+Gone', which is not in this config set"));
        }

        [Test]
        public void Run_JsonReferenceOfWrongType_ReportsError()
        {
            TestPinnedTemplate pinned = Create<TestPinnedTemplate>("P");
            JsonTemplateFile file = Json(TestPayloads.Item("TestItemTemplate+A", upgrade: "TestPinnedTemplate+P"));

            ConfigIssue error = SingleError(Run(new[] { pinned }, file));

            Assert.That(error.Message, Does.Contain(nameof(TestPinnedTemplate)).And.Contain("doesn't match"));
        }

        [Test]
        public void Run_AssetReferenceOutsideSet_ReportsError()
        {
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            ((ITemplateReference)sword.Upgrade).Template = Create<TestItemTemplate>("Elsewhere");

            ConfigIssue error = SingleError(Run(new[] { sword }));

            Assert.That(error.Message,
                Does.Contain("TestItemTemplate+Sword").And.Contain("Elsewhere").And.Contain("Upgrade")
                    .And.Contain("not in this config set"));
        }

        [Test]
        public void Run_UnaddressableAsset_ReportsErrorWithFix()
        {
            Sprite icon = CreateSprite();
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            ((ITemplateAssetReference)sword.Icon).Asset = icon;

            ConfigIssue error = SingleError(Run(new[] { sword }));
            error.Fix();

            Assert.That(error.Message, Does.Contain("not addressable").And.Contain("Icon"));
            Assert.That(_addresses.MadeAddressable, Is.EqualTo(new[] { icon }));
        }

        [Test]
        public void Run_AddressableAsset_WritesAddress()
        {
            Sprite icon = CreateSprite();
            _addresses.Addresses[icon] = "Icons/Sword";
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            ((ITemplateAssetReference)sword.Icon).Asset = icon;

            ConfigBuildResult result = Run(new[] { sword });

            Assert.That(((ITemplateAssetReference)sword.Icon).Address, Is.EqualTo("Icons/Sword"));
            Assert.That(Entry(result, "TestItemTemplate+Sword")["Icon"].Value<string>(), Is.EqualTo("Icons/Sword"));
        }

        [Test]
        public void Run_ClearedAsset_ClearsAddress()
        {
            TestItemTemplate sword = Create<TestItemTemplate>("Sword");
            ((ITemplateAssetReference)sword.Icon).Address = "Icons/Old";

            ConfigBuildResult result = Run(new[] { sword });

            Assert.That(Entry(result, "TestItemTemplate+Sword")["Icon"].Type, Is.EqualTo(JTokenType.Null));
        }

        [Test]
        public void Run_EnumValueWithoutTemplate_Warns()
        {
            TestStatTemplate health = Create<TestStatTemplate>("Health");

            ConfigBuildResult result = Run(new[] { health });

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Payload, Is.Not.Null);
            Assert.That(result.Issues.Single().Message, Does.Contain("TestStat.Mana").And.Contain("TestStatTemplate+Mana"));
        }

        [Test]
        public void Run_TypeIdentifierCollision_ReportsError()
        {
            ConfigBuildResult result = Run(new Template[]
            {
                Create<TestPinnedTemplate>("A"), Create<TestPinnedTwinTemplate>("B"),
            });

            Assert.That(SingleError(result).Message, Does.Contain("'Pinned'").And.Contain(nameof(TestPinnedTwinTemplate)));
        }

        [Test]
        public void Run_InputOrder_DoesNotChangePayload()
        {
            TestItemTemplate a = Create<TestItemTemplate>("A");
            TestWeaponTemplate b = Create<TestWeaponTemplate>("B");
            JsonTemplateFile file = Json(TestPayloads.Item("TestItemTemplate+C"), TestPayloads.Item("TestItemTemplate+D"));
            JsonTemplateFile reversed = Json(TestPayloads.Item("TestItemTemplate+D"), TestPayloads.Item("TestItemTemplate+C"));

            string first = Run(new Template[] { a, b }, file).Payload.ToString();
            string second = Run(new Template[] { b, a }, reversed).Payload.ToString();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(Ids(Run(new Template[] { b, a }, file)),
                Is.EqualTo(new[] { "TestItemTemplate+A", "TestItemTemplate+C", "TestItemTemplate+D", "TestWeaponTemplate+B" }));
        }

        [Test]
        public void Run_Payload_ReadsBackWithTemplateTypes()
        {
            TestWeaponTemplate bow = Create<TestWeaponTemplate>("Bow");
            bow.Cooldown = 0f;
            JsonTemplateFile file = Json(TestPayloads.Item("TestItemTemplate+Arrow"));

            ConfigBuildResult result = Run(new[] { bow }, file);
            ConfigTypeMapping mapping = ConfigTypeMapping.Create(result.TemplateTypes.ToArray());
            _objects.Add(mapping);
            ConfigPayload payload = ConfigSerialization.Deserialize(mapping, result.Payload);
            _objects.AddRange(payload.Templates);

            Assert.That(result.TemplateTypes, Is.EqualTo(new[] { typeof(TestItemTemplate), typeof(TestWeaponTemplate) }));
            Assert.That(payload.Templates.Select(template => template.ID),
                Is.EqualTo(new[] { "TestItemTemplate+Arrow", "TestWeaponTemplate+Bow" }));
            Assert.That(((TestWeaponTemplate)payload.Templates[1]).Cooldown, Is.EqualTo(0f));
        }

        private ConfigBuildResult Run(IEnumerable<Template> assets, params JsonTemplateFile[] files)
        {
            return ConfigBuild.Run(assets, files, _addresses);
        }

        private static JsonTemplateFile Json(params JObject[] templates)
        {
            return new JsonTemplateFile("Assets/Test.json", new JArray(templates.Cast<object>().ToArray()).ToString());
        }

        private static JObject Entry(ConfigBuildResult result, string id)
        {
            Assert.That(result.Payload, Is.Not.Null, string.Join("\n", result.Issues));
            return result.Payload["templates"].Cast<JObject>().Single(entry => entry["ID"].Value<string>() == id);
        }

        private static IEnumerable<string> Ids(ConfigBuildResult result)
        {
            Assert.That(result.Payload, Is.Not.Null, string.Join("\n", result.Issues));
            return result.Payload["templates"].Select(entry => entry["ID"].Value<string>());
        }

        private static ConfigIssue SingleError(ConfigBuildResult result)
        {
            Assert.That(result.Issues, Has.Count.EqualTo(1), string.Join("\n", result.Issues));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(ConfigIssueSeverity.Error));
            Assert.That(result.Payload, Is.Null);
            return result.Issues[0];
        }

        private T Create<T>(string name) where T : Template
        {
            T template = ScriptableObject.CreateInstance<T>();
            template.name = name;
            _objects.Add(template);
            return template;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new(1, 1);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            _objects.Add(sprite);
            _objects.Add(texture);
            return sprite;
        }

        private sealed class FakeAddressResolver : IAssetAddressResolver
        {
            public Dictionary<Object, string> Addresses { get; } = new();
            public List<Object> MadeAddressable { get; } = new();

            public string GetAddress(Object asset) => Addresses.TryGetValue(asset, out string address) ? address : null;

            public void MakeAddressable(Object asset) => MadeAddressable.Add(asset);
        }
    }
}
