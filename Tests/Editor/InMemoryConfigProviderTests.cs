using Kott.Configs.Testing;
using NUnit.Framework;

namespace Kott.Configs.Tests
{
    public class InMemoryConfigProviderTests
    {
        private InMemoryConfigProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new InMemoryConfigProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
        }

        [Test]
        public void Add_Templates_LookupsMatchCatalog()
        {
            TestItemTemplate item = _provider.Add<TestItemTemplate>("TestItem+A", template => template.Damage = 2);
            TestWeaponTemplate weapon = _provider.Add<TestWeaponTemplate>("TestWeapon+B");
            _provider.AddType<TestUnusedTemplate>();

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(2));
            Assert.That(_provider.GetCollection<TestItemTemplate>(), Is.EqualTo(new TestItemTemplate[] { item, weapon }));
            Assert.That(_provider.GetCollection<TestUnusedTemplate>(), Is.Empty);
        }

        [Test]
        public void Add_AfterQuery_IsVisible()
        {
            _provider.Add<TestItemTemplate>("TestItem+A");
            Assert.That(_provider.GetCollection<TestItemTemplate>(), Has.Length.EqualTo(1));

            _provider.Add<TestItemTemplate>("TestItem+B");

            Assert.That(_provider.GetCollection<TestItemTemplate>(), Has.Length.EqualTo(2));
        }

        [Test]
        public void Dispose_CreatedTemplates_Destroyed()
        {
            TestItemTemplate item = _provider.Add<TestItemTemplate>("TestItem+A");

            _provider.Dispose();

            Assert.That(item == null, Is.True);
        }

        [Test]
        public void GetTemplate_EnumValue_ReturnsTemplate()
        {
            TestStatTemplate health = _provider.Add<TestStatTemplate>("TestStatTemplate+Health");

            Assert.That(_provider.GetTemplate<TestStatTemplate>(TestStat.Health), Is.SameAs(health));
        }
    }
}
