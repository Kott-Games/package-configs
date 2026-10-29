using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Kott.Configs.Tests
{
    public class ConfigEditorUtilityTests
    {
        [Test]
        public void GetTemplateTypeTree_NoFilter_ListsSubtypesBelowTheirBaseType()
        {
            List<(Type Type, int Depth)> tree = ConfigEditorUtility.GetTemplateTypeTree();

            int item = tree.FindIndex(entry => entry.Type == typeof(TestItemTemplate));
            Assert.That(tree[item].Depth, Is.EqualTo(0));
            Assert.That(tree.Skip(item + 1).TakeWhile(entry => entry.Depth > 0),
                Does.Contain((typeof(TestWeaponTemplate), 1)));
        }

        [Test]
        public void GetTemplateTypeTree_IncludedTypes_KeepsOnlyThemAndTheirBaseTypes()
        {
            List<(Type Type, int Depth)> tree =
                ConfigEditorUtility.GetTemplateTypeTree(new[] { typeof(TestWeaponTemplate) });

            Assert.That(tree, Is.EqualTo(new (Type, int)[]
            {
                (typeof(TestItemTemplate), 0),
                (typeof(TestWeaponTemplate), 1),
            }));
        }
    }
}
