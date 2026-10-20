using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kott.Configs.Tests
{
    public class TemplateCatalogTests
    {
        private readonly List<Template> _templates = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Template template in _templates)
            {
                Object.DestroyImmediate(template);
            }

            _templates.Clear();
        }

        [Test]
        public void GetCollection_BaseType_IncludesSubtypesInOrder()
        {
            TestItemTemplate item = Create<TestItemTemplate>("TestItem+A");
            TestWeaponTemplate weapon = Create<TestWeaponTemplate>("TestWeapon+B");
            TemplateCatalog catalog = new(_templates);

            Assert.That(catalog.GetCollection<TestItemTemplate>(), Is.EqualTo(new TestItemTemplate[] { item, weapon }));
            Assert.That(catalog.GetCollection<TestWeaponTemplate>(), Is.EqualTo(new[] { weapon }));
            Assert.That(catalog.GetCollection<Template>(), Is.EqualTo(new Template[] { item, weapon }));
        }

        [Test]
        public void GetCollection_KnownTypeWithoutTemplates_ReturnsEmpty()
        {
            TemplateCatalog catalog = new(_templates, new[] { typeof(TestUnusedTemplate) });

            Assert.That(catalog.GetCollection<TestUnusedTemplate>(), Is.Empty);
        }

        [Test]
        public void GetCollection_UnknownType_ThrowsNamingType()
        {
            TemplateCatalog catalog = new(_templates);

            KeyNotFoundException exception =
                Assert.Throws<KeyNotFoundException>(() => catalog.GetCollection<TestUnusedTemplate>());

            Assert.That(exception.Message, Does.Contain(nameof(TestUnusedTemplate)));
        }

        [Test]
        public void GetTemplate_ExistingId_ReturnsTemplate()
        {
            TestWeaponTemplate weapon = Create<TestWeaponTemplate>("TestWeapon+B");
            TemplateCatalog catalog = new(_templates);

            Assert.That(catalog.GetTemplate<TestItemTemplate>("TestWeapon+B"), Is.SameAs(weapon));
        }

        [Test]
        public void GetTemplate_MissingId_ThrowsNamingId()
        {
            TemplateCatalog catalog = new(_templates);

            KeyNotFoundException exception =
                Assert.Throws<KeyNotFoundException>(() => catalog.GetTemplate<TestItemTemplate>("TestItem+Missing"));

            Assert.That(exception.Message, Does.Contain("TestItem+Missing"));
        }

        [Test]
        public void GetTemplate_WrongType_ThrowsNamingTypes()
        {
            Create<TestItemTemplate>("TestItem+A");
            TemplateCatalog catalog = new(_templates);

            InvalidCastException exception =
                Assert.Throws<InvalidCastException>(() => catalog.GetTemplate<TestWeaponTemplate>("TestItem+A"));

            Assert.That(exception.Message, Does.Contain(nameof(TestItemTemplate)).And.Contain(nameof(TestWeaponTemplate)));
        }

        [Test]
        public void TryGetTemplate_WrongTypeOrMissing_ReturnsFalse()
        {
            TestItemTemplate item = Create<TestItemTemplate>("TestItem+A");
            TemplateCatalog catalog = new(_templates);

            Assert.That(catalog.TryGetTemplate("TestItem+A", out TestItemTemplate found), Is.True);
            Assert.That(found, Is.SameAs(item));
            Assert.That(catalog.TryGetTemplate("TestItem+A", out TestWeaponTemplate _), Is.False);
            Assert.That(catalog.TryGetTemplate("TestItem+Missing", out TestItemTemplate _), Is.False);
        }

        [Test]
        public void Constructor_DuplicateId_ThrowsNamingId()
        {
            Create<TestItemTemplate>("TestItem+A");
            Create<TestWeaponTemplate>("TestItem+A");

            ConfigException exception = Assert.Throws<ConfigException>(() => new TemplateCatalog(_templates));

            Assert.That(exception.Message, Does.Contain("TestItem+A"));
        }

        [Test]
        public void Constructor_MissingId_Throws()
        {
            Create<TestItemTemplate>(null);

            Assert.Throws<ConfigException>(() => new TemplateCatalog(_templates));
        }

        [Test]
        public void GetTemplate_EnumValue_ReturnsTemplateWithDefaultIdFormat()
        {
            TestStatTemplate health = Create<TestStatTemplate>("TestStatTemplate+Health");
            TestStatTemplate mana = Create<TestStatTemplate>("TestStatTemplate+Mana");
            TemplateCatalog catalog = new(_templates);

            Assert.That(catalog.GetTemplate<TestStatTemplate>(TestStat.Health), Is.SameAs(health));
            Assert.That(catalog.GetTemplate<TestStatTemplate>(TestStat.Mana), Is.SameAs(mana));
        }

        [Test]
        public void GetTemplate_EnumValueWithIdFormat_ReturnsTemplate()
        {
            TestFormattedStatTemplate health = Create<TestFormattedStatTemplate>("stats/Health");
            TemplateCatalog catalog = new(_templates);

            Assert.That(catalog.GetTemplate<TestFormattedStatTemplate>(TestStat.Health), Is.SameAs(health));
        }

        [Test]
        public void GetTemplate_EnumValueWithoutTemplate_ThrowsNamingExpectedId()
        {
            Create<TestStatTemplate>("TestStatTemplate+Health");
            TemplateCatalog catalog = new(_templates);

            KeyNotFoundException exception =
                Assert.Throws<KeyNotFoundException>(() => catalog.GetTemplate<TestStatTemplate>(TestStat.Mana));

            Assert.That(exception.Message, Does.Contain("TestStat.Mana").And.Contain("TestStatTemplate+Mana"));
        }

        [Test]
        public void GetTemplate_EnumWithoutMapping_ThrowsArgument()
        {
            Create<TestItemTemplate>("TestItem+A");
            TemplateCatalog catalog = new(_templates);

            Assert.Throws<ArgumentException>(() => catalog.GetTemplate<TestItemTemplate>(TestStat.Health));
            Assert.Throws<ArgumentException>(() => catalog.GetTemplate<TestStatTemplate>(LogType.Error));
        }

        private T Create<T>(string id) where T : Template
        {
            T template = ScriptableObject.CreateInstance<T>();
            template.ID = id;
            _templates.Add(template);
            return template;
        }
    }
}
