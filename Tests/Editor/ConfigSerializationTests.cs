using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Kott.Configs.Tests
{
    public class ConfigSerializationTests
    {
        private readonly List<Object> _objects = new();
        private ConfigTypeMapping _mapping;

        [SetUp]
        public void SetUp()
        {
            _mapping = TestPayloads.Mapping();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object target in _objects)
            {
                Object.DestroyImmediate(target);
            }

            _objects.Clear();
            Object.DestroyImmediate(_mapping);
        }

        [Test]
        public void Serialize_Template_WritesFormatVersionTypeAndFields()
        {
            TestItemTemplate item = Create<TestItemTemplate>("TestItem+Sword");
            item.Damage = 5;

            JObject payload = ConfigSerialization.Serialize(_mapping, new[] { item });

            Assert.That(payload["formatVersion"].Value<int>(), Is.EqualTo(ConfigSerialization.FormatVersion));
            JObject jItem = (JObject)payload["templates"][0];
            Assert.That(jItem.Properties().First().Name, Is.EqualTo("Type"));
            Assert.That(jItem["Type"].Value<string>(), Is.EqualTo("TestItem"));
            Assert.That(jItem["ID"].Value<string>(), Is.EqualTo("TestItem+Sword"));
            Assert.That(jItem["Damage"].Value<int>(), Is.EqualTo(5));
            Assert.That(jItem["name"], Is.Null);
            Assert.That(jItem["hideFlags"], Is.Null);
        }

        [Test]
        public void Serialize_PinnedType_WritesPinnedIdentifier()
        {
            TestPinnedTemplate pinned = Create<TestPinnedTemplate>("TestPinned+A");

            JObject payload = ConfigSerialization.Serialize(_mapping, new[] { pinned });

            Assert.That(payload["templates"][0]["Type"].Value<string>(), Is.EqualTo("Pinned"));
        }

        [Test]
        public void Serialize_UnmappedType_ThrowsNamingTemplate()
        {
            TestFormattedStatTemplate unmapped = Create<TestFormattedStatTemplate>("stats/Health");

            ConfigException exception = Assert.Throws<ConfigException>(() =>
                ConfigSerialization.Serialize(_mapping, new[] { unmapped }));

            Assert.That(exception.Message, Does.Contain("stats/Health").And.Contain(nameof(TestFormattedStatTemplate)));
        }

        [Test]
        public void Serialize_SameTemplates_IsDeterministic()
        {
            TestItemTemplate item = Create<TestItemTemplate>("TestItem+Sword");
            item.Tags.Add("sharp");

            string first = ConfigSerialization.Serialize(_mapping, new[] { item }).ToString();
            string second = ConfigSerialization.Serialize(_mapping, new[] { item }).ToString();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Deserialize_SerializedTemplates_RoundTripsData()
        {
            TestWeaponTemplate weapon = Create<TestWeaponTemplate>("TestWeapon+Bow");
            weapon.Damage = 3;
            weapon.Range = 12.5f;
            weapon.Offset = new Vector3(1f, 2f, 3f);
            weapon.Tags.AddRange(new[] { "ranged", "wood" });
            ((ITemplateAssetReference)weapon.Icon).Address = "Icons/Bow";
            TestItemTemplate arrow = Create<TestItemTemplate>("TestItem+Arrow");
            ((ITemplateReference)weapon.Upgrade).Template = arrow;

            string json = ConfigSerialization.Serialize(_mapping, new TestItemTemplate[] { weapon, arrow }).ToString();
            ConfigPayload payload = Track(ConfigSerialization.Deserialize(_mapping, json));

            TestWeaponTemplate read = (TestWeaponTemplate)payload.Templates[0];
            Assert.That(read.ID, Is.EqualTo("TestWeapon+Bow"));
            Assert.That(read.Damage, Is.EqualTo(3));
            Assert.That(read.Range, Is.EqualTo(12.5f));
            Assert.That(read.Offset, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(read.Tags, Is.EqualTo(new[] { "ranged", "wood" }));
            Assert.That(((ITemplateAssetReference)read.Icon).Address, Is.EqualTo("Icons/Bow"));
            Assert.That(payload.References.Single().TargetId, Is.EqualTo("TestItem+Arrow"));
            Assert.That(payload.References.Single().Owner, Is.SameAs(read));
            Assert.That(payload.Templates[1], Is.TypeOf<TestItemTemplate>());
        }

        [TestCase(null)]
        [TestCase("")]
        public void Serialize_AssetReferenceWithoutAddress_WritesNull(string address)
        {
            TestWeaponTemplate weapon = Create<TestWeaponTemplate>("TestWeapon+Bow");
            ((ITemplateAssetReference)weapon.Icon).Address = address;

            JObject jWeapon = (JObject)ConfigSerialization.Serialize(_mapping, new[] { weapon })["templates"][0];

            Assert.That(jWeapon["Icon"].Type, Is.EqualTo(JTokenType.Null));
        }

        [Test]
        public void Serialize_DefaultValueOverInitializer_WritesValue()
        {
            TestWeaponTemplate weapon = Create<TestWeaponTemplate>("TestWeapon+Bow");
            weapon.Cooldown = 0f;
            weapon.Stackable = false;

            JObject jWeapon = (JObject)ConfigSerialization.Serialize(_mapping, new[] { weapon })["templates"][0];

            Assert.That(jWeapon["Cooldown"]?.Value<float>(), Is.EqualTo(0f));
            Assert.That(jWeapon["Stackable"]?.Value<bool>(), Is.False);
        }

        [Test]
        public void Deserialize_DefaultValueOverInitializer_KeepsValue()
        {
            JObject weapon = TestPayloads.Item("TestWeapon+Bow", type: "TestWeapon");
            weapon["Cooldown"] = 0f;
            weapon["Stackable"] = false;

            ConfigPayload payload = Track(ConfigSerialization.Deserialize(_mapping, TestPayloads.Payload(weapon)));

            TestWeaponTemplate read = (TestWeaponTemplate)payload.Templates.Single();
            Assert.That(read.Cooldown, Is.EqualTo(0f));
            Assert.That(read.Stackable, Is.False);
        }

        [Test]
        public void Deserialize_MissingField_KeepsInitializer()
        {
            JObject weapon = TestPayloads.Item("TestWeapon+Bow", type: "TestWeapon");

            ConfigPayload payload = Track(ConfigSerialization.Deserialize(_mapping, TestPayloads.Payload(weapon)));

            TestWeaponTemplate read = (TestWeaponTemplate)payload.Templates.Single();
            Assert.That(read.Cooldown, Is.EqualTo(1.5f));
            Assert.That(read.Stackable, Is.True);
        }

        [Test]
        public void Deserialize_FlatArray_ReadsTemplates()
        {
            string json = new JArray(TestPayloads.Item("TestItem+A"), TestPayloads.Item("TestItem+B")).ToString();

            ConfigPayload payload = Track(ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(payload.Templates.Select(template => template.ID),
                Is.EqualTo(new[] { "TestItem+A", "TestItem+B" }));
        }

        [Test]
        public void Deserialize_NewerFormatVersion_ThrowsNamingVersions()
        {
            string json = new JObject { { "formatVersion", 99 }, { "templates", new JArray() } }.ToString();

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("99").And.Contain("Update the Configs package"));
        }

        [Test]
        public void Deserialize_ObjectWithoutFormatVersion_Throws()
        {
            string json = new JObject { { "templates", new JArray() } }.ToString();

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("formatVersion"));
        }

        [TestCase("not json")]
        [TestCase("{ \"formatVersion\": 1, ")]
        public void Deserialize_InvalidJson_Throws(string json)
        {
            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("not valid JSON"));
        }

        [Test]
        public void Deserialize_ScalarPayload_Throws()
        {
            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, "42"));

            Assert.That(exception.Message, Does.Contain("object or array"));
        }

        [Test]
        public void Deserialize_UnknownType_ThrowsNamingTypeAndTemplate()
        {
            string json = TestPayloads.Payload(TestPayloads.Item("Renamed+A", type: "Renamed"));

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("Renamed+A").And.Contain("unknown type 'Renamed'")
                .And.Contain("[TemplateType]"));
        }

        [Test]
        public void Deserialize_DuplicateId_ThrowsNamingId()
        {
            string json = TestPayloads.Payload(TestPayloads.Item("TestItem+A"), TestPayloads.Item("TestItem+A"));

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("Duplicate template ID 'TestItem+A'"));
        }

        [Test]
        public void Deserialize_MissingId_ThrowsNamingEntry()
        {
            string json = TestPayloads.Payload(new JObject { { "Type", "TestItem" } });

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("#0").And.Contain("ID"));
        }

        [Test]
        public void Deserialize_MissingType_ThrowsNamingTemplate()
        {
            string json = TestPayloads.Payload(new JObject { { "ID", "TestItem+A" } });

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("TestItem+A").And.Contain("Type"));
        }

        [Test]
        public void Deserialize_AbstractType_Throws()
        {
            _mapping.Add(typeof(TestAbstractTemplate));
            string json = TestPayloads.Payload(TestPayloads.Item("A", type: "TestAbstract"));

            ConfigException exception =
                Assert.Throws<ConfigException>(() => ConfigSerialization.Deserialize(_mapping, json));

            Assert.That(exception.Message, Does.Contain("abstract"));
        }

        [Test]
        public void Deserialize_InvalidFieldValue_ThrowsNamingTemplate()
        {
            JObject item = TestPayloads.Item("TestItem+A");
            item["Damage"] = "lots";

            ConfigException exception = Assert.Throws<ConfigException>(() =>
                ConfigSerialization.Deserialize(_mapping, TestPayloads.Payload(item)));

            Assert.That(exception.Message, Does.Contain("TestItem+A"));
        }

        [Test]
        public void Deserialize_ReferenceNotAString_ThrowsNamingTemplate()
        {
            JObject item = TestPayloads.Item("TestItem+A");
            item["Upgrade"] = new JObject();

            ConfigException exception = Assert.Throws<ConfigException>(() =>
                ConfigSerialization.Deserialize(_mapping, TestPayloads.Payload(item)));

            Assert.That(exception.Message, Does.Contain("TestItem+A").And.Contain("template ID"));
        }

        private T Create<T>(string id) where T : Template
        {
            T template = ScriptableObject.CreateInstance<T>();
            template.ID = id;
            _objects.Add(template);
            return template;
        }

        private ConfigPayload Track(ConfigPayload payload)
        {
            _objects.AddRange(payload.Templates);
            return payload;
        }
    }
}
