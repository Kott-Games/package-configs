using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kott.Configs.Tests
{
    public class TemplateReferenceTests
    {
        [Test]
        public void ImplicitConversion_NullReference_ReturnsNull()
        {
            TemplateReference<TestItemTemplate> reference = null;

            TestItemTemplate template = reference;

            Assert.That(template, Is.Null);
        }

        [Test]
        public void ImplicitConversion_SetReference_ReturnsTemplate()
        {
            TestItemTemplate target = ScriptableObject.CreateInstance<TestItemTemplate>();
            TemplateReference<TestItemTemplate> reference = new();
            ((ITemplateReference)reference).Template = target;

            TestItemTemplate template = reference;

            Assert.That(template, Is.SameAs(target));
            Object.DestroyImmediate(target);
        }

        [Test]
        public void Template_WrongTypeAssigned_IsNull()
        {
            TestItemTemplate item = ScriptableObject.CreateInstance<TestItemTemplate>();
            TemplateReference<TestWeaponTemplate> reference = new();
            ((ITemplateReference)reference).Template = item;

            Assert.That(reference.Template, Is.Null);
            Object.DestroyImmediate(item);
        }
    }
}
