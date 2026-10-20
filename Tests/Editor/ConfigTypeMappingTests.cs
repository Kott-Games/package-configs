using System;
using System.Collections.Generic;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace Kott.Configs.Tests
{
    public class ConfigTypeMappingTests
    {
        private ConfigTypeMapping _mapping;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mapping);
        }

        [Test]
        public void Create_SubType_MapsTypeAndBaseTypes()
        {
            _mapping = ConfigTypeMapping.Create(typeof(TestWeaponTemplate));

            Dictionary<string, Type> types = _mapping.ResolveTypes();

            Assert.That(types, Is.EquivalentTo(new Dictionary<string, Type>
            {
                { "TestWeapon", typeof(TestWeaponTemplate) },
                { "TestItem", typeof(TestItemTemplate) },
            }));
            Assert.That(_mapping.TypeGroups[0].BaseTypeStrings, Is.EqualTo(new[]
            {
                typeof(TestWeaponTemplate).AssemblyQualifiedName,
                typeof(TestItemTemplate).AssemblyQualifiedName,
                typeof(Template).AssemblyQualifiedName,
            }));
        }

        [Test]
        public void Add_MappedType_AddsNothing()
        {
            _mapping = ConfigTypeMapping.Create(typeof(TestWeaponTemplate));

            _mapping.Add(typeof(TestItemTemplate));

            Assert.That(_mapping.TypeGroups, Has.Count.EqualTo(2));
        }

        [Test]
        public void Add_NonTemplateType_Throws()
        {
            _mapping = ConfigTypeMapping.Create();

            Assert.Throws<ArgumentException>(() => _mapping.Add(typeof(string)));
        }

        [Test]
        public void ResolveTypes_TypeNoLongerExists_ThrowsNamingType()
        {
            _mapping = ConfigTypeMapping.Create();
            _mapping.TypeGroups.Add(new ConfigTypeMapping.Group { TypeIdentifier = "Gone", TypeString = "Missing.GoneTemplate, Missing" });

            ConfigException exception = Assert.Throws<ConfigException>(() => _mapping.ResolveTypes());

            Assert.That(exception.Message, Does.Contain("Missing.GoneTemplate").And.Contain("Gone"));
        }

        [Test]
        public void ResolveTypes_DuplicateIdentifier_ThrowsNamingTypes()
        {
            _mapping = ConfigTypeMapping.Create(typeof(TestItemTemplate));
            _mapping.TypeGroups.Add(new ConfigTypeMapping.Group
            {
                TypeIdentifier = "TestItem", TypeString = typeof(TestUnusedTemplate).AssemblyQualifiedName,
            });

            ConfigException exception = Assert.Throws<ConfigException>(() => _mapping.ResolveTypes());

            Assert.That(exception.Message, Does.Contain("'TestItem'").And.Contain(nameof(TestUnusedTemplate)));
        }

        [Test]
        public void GetTypeIdentifier_Default_IsClassNameWithoutTemplate()
        {
            Assert.That(TemplateIdUtility.GetTypeIdentifier(typeof(TestItemTemplate)), Is.EqualTo("TestItem"));
        }

        [Test]
        public void GetTypeIdentifier_TemplateTypeAttribute_IsPinnedIdentifier()
        {
            Assert.That(TemplateIdUtility.GetTypeIdentifier(typeof(TestPinnedTemplate)), Is.EqualTo("Pinned"));
        }

        [Test]
        public void CreateId_NoGroupAttribute_UsesTypeName()
        {
            Assert.That(TemplateIdUtility.CreateId(typeof(TestItemTemplate), "Sword"), Is.EqualTo("TestItemTemplate+Sword"));
        }
    }
}
