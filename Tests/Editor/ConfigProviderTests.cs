using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Kott.Configs.Tests.TestPayloads;
using Object = UnityEngine.Object;

namespace Kott.Configs.Tests
{
    public class ConfigProviderTests
    {
        private RecordingLogHandler _log;
        private ConfigTypeMapping _mapping;
        private ConfigProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _log = new RecordingLogHandler();
            _mapping = Mapping();
        }

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
            _provider = null;
            Object.DestroyImmediate(_mapping);
            _log.Verify();
        }

        [Test]
        public void LoadAsync_SingleLoader_LoadsTemplatesAndKnownTypes()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A", 1), Item("TestWeapon+B", type: "TestWeapon"))));

            Assert.That(_provider.IsLoaded, Is.True);
            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(1));
            Assert.That(_provider.GetCollection<TestItemTemplate>(), Has.Count.EqualTo(2));
            Assert.That(_provider.GetCollection<TestUnusedTemplate>(), Is.Empty);
        }

        [Test]
        public void LoadAsync_References_ResolvedToLoadedTemplates()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A", upgrade: "TestItem+B"), Item("TestItem+B"))));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Upgrade.Template,
                Is.SameAs(_provider.GetTemplate<TestItemTemplate>("TestItem+B")));
        }

        [Test]
        public void LoadAsync_ReferenceCycle_Resolved()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A", upgrade: "TestItem+B"), Item("TestItem+B", upgrade: "TestItem+A"))));

            TestItemTemplate a = _provider.GetTemplate<TestItemTemplate>("TestItem+A");
            Assert.That(a.Upgrade.Template.Upgrade.Template, Is.SameAs(a));
        }

        [Test]
        public void LoadAsync_LaterLoader_OverridesTemplatesByIdKeepingOrder()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A", 1), Item("TestItem+B", 1))),
                new FakeLoader(Payload(Item("TestItem+A", 2), Item("TestItem+C", 2))));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(2));
            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+B").Damage, Is.EqualTo(1));
            Assert.That(_provider.GetCollection<TestItemTemplate>(), Has.Count.EqualTo(3));
            Assert.That(_provider.GetCollection<TestItemTemplate>()[0].ID, Is.EqualTo("TestItem+A"));
        }

        [Test]
        public void LoadAsync_ReferenceToOverriddenTemplate_ResolvesToOverride()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A", upgrade: "TestItem+B"), Item("TestItem+B", 1))),
                new FakeLoader(Payload(Item("TestItem+B", 2))));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Upgrade.Template.Damage, Is.EqualTo(2));
        }

        [Test]
        public void LoadAsync_OverriddenTemplate_ItsReferencesIgnored()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A", upgrade: "TestItem+Missing"))),
                new FakeLoader(Payload(Item("TestItem+A"))));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Upgrade.Template, Is.Null);
        }

        [Test]
        public void LoadAsync_DanglingReference_WarnsNamingBothIds()
        {
            _log.Expect(LogType.Warning, @"'TestItem\+A' references 'TestItem\+Missing'.*doesn't exist");

            Load(new FakeLoader(Payload(Item("TestItem+A", upgrade: "TestItem+Missing"))));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Upgrade.Template, Is.Null);
        }

        [Test]
        public void LoadAsync_ReferenceOfWrongType_WarnsNamingBothIds()
        {
            _log.Expect(LogType.Warning, @"'TestItem\+A' references 'TestStat\+Health'.*TestStatTemplate");

            Load(new FakeLoader(Payload(Item("TestItem+A", upgrade: "TestStat+Health"), Stat("TestStat+Health", 1f))));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Upgrade.Template, Is.Null);
        }

        [Test]
        public void LoadAsync_RequiredLoaderFails_ThrowsNamingLoader()
        {
            _provider = new ConfigProvider(_mapping, new[] { FakeLoader.Failing(new IOException("disk on fire")) },
                _log.Logger);

            ConfigException exception = Assert.Throws<ConfigException>(() => Run(_provider.LoadAsync()));

            Assert.That(exception.Message, Does.Contain(nameof(FakeLoader)).And.Contain("disk on fire"));
            Assert.That(_provider.IsLoaded, Is.False);
        }

        [Test]
        public void LoadAsync_RequiredLoaderGarbagePayload_Throws()
        {
            _provider = new ConfigProvider(_mapping, new[] { new FakeLoader("<html>") }, _log.Logger);

            ConfigException exception = Assert.Throws<ConfigException>(() => Run(_provider.LoadAsync()));

            Assert.That(exception.Message, Does.Contain("not valid JSON"));
        }

        [Test]
        public void LoadAsync_OptionalLoaderFails_WarnsAndContinues()
        {
            _log.Expect(LogType.Warning, "Optional config loader FakeLoader failed.*offline");

            Load(new FakeLoader(Payload(Item("TestItem+A", 1))),
                FakeLoader.Failing(new IOException("offline"), ConfigLoaderPolicy.Optional));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(1));
        }

        [Test]
        public void LoadAsync_OptionalLoaderGarbagePayload_WarnsAndContinues()
        {
            _log.Expect(LogType.Warning, "Optional config loader FakeLoader failed.*Duplicate");

            Load(new FakeLoader(Payload(Item("TestItem+A", 1))),
                new FakeLoader(Payload(Item("TestItem+A", 2), Item("TestItem+A", 3)), ConfigLoaderPolicy.Optional));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(1));
        }

        [Test]
        public void LoadAsync_LoaderReturnsNull_Skipped()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A"))), new FakeLoader((string)null));

            Assert.That(_provider.GetCollection<TestItemTemplate>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void LoadAsync_Verbose_LogsCountsNotPayload()
        {
            _provider = new ConfigProvider(_mapping, new[] { new FakeLoader(Payload(Item("TestItem+A", 4711))) },
                _log.Logger) { Verbose = true };

            Run(_provider.LoadAsync());

            Assert.That(_log.Entries, Has.Some.Matches<(LogType Type, string Message)>(entry =>
                entry.Message.Contains("1 templates")));
            Assert.That(_log.Entries, Has.None.Matches<(LogType Type, string Message)>(entry =>
                entry.Message.Contains("4711")));
        }

        [UnityTest]
        public IEnumerator LoadAsync_RequiredLoaderTimesOut_ThrowsAndCancelsLoader()
        {
            FakeLoader loader = new(_ => new TaskCompletionSource<string>().Task) { Timeout = 0.05f };
            _provider = new ConfigProvider(_mapping, new[] { loader }, _log.Logger);

            Task load = _provider.LoadAsync();
            float giveUp = Time.realtimeSinceStartup + 5f;
            while (!load.IsCompleted && Time.realtimeSinceStartup < giveUp)
            {
                yield return null;
            }

            Assert.That(load.IsFaulted, Is.True);
            Assert.That(load.Exception.InnerException, Is.TypeOf<ConfigException>());
            Assert.That(load.Exception.InnerException.Message, Does.Contain("Timed out"));
            Assert.That(loader.Token.IsCancellationRequested, Is.True);
        }

        [Test]
        public void LoadAsync_Cancelled_ThrowsOperationCanceled()
        {
            using CancellationTokenSource cancellation = new();
            FakeLoader loader = new(token =>
            {
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.FromResult(Payload());
            });
            _provider = new ConfigProvider(_mapping, new[] { loader }, _log.Logger);

            Assert.Catch<OperationCanceledException>(() => Run(_provider.LoadAsync(null, cancellation.Token)));
        }

        [Test]
        public void LoadAsync_Reload_ReplacesTemplatesAndDestroysOldOnes()
        {
            string json = Payload(Item("TestItem+A", 1));
            FakeLoader loader = new(_ => Task.FromResult(json));
            Load(loader);
            TestItemTemplate old = _provider.GetTemplate<TestItemTemplate>("TestItem+A");

            json = Payload(Item("TestItem+A", 2));
            Run(_provider.LoadAsync());

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(2));
            Assert.That(old == null, Is.True, "old template destroyed");
        }

        [Test]
        public void LoadAsync_ReloadFails_KeepsPreviousTemplates()
        {
            bool fail = false;
            FakeLoader loader = new(_ => fail
                ? Task.FromException<string>(new IOException("gone"))
                : Task.FromResult(Payload(Item("TestItem+A", 1))));
            Load(loader);

            fail = true;
            Assert.Throws<ConfigException>(() => Run(_provider.LoadAsync()));

            Assert.That(_provider.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(1));
        }

        [Test]
        public void GetTemplate_BeforeLoad_ThrowsInvalidOperation()
        {
            _provider = new ConfigProvider(_mapping, Array.Empty<ConfigLoader>(), _log.Logger);

            Assert.Throws<InvalidOperationException>(() => _provider.GetTemplate<TestItemTemplate>("TestItem+A"));
        }

        [Test]
        public void Dispose_Loaded_DestroysTemplates()
        {
            Load(new FakeLoader(Payload(Item("TestItem+A"))));
            TestItemTemplate item = _provider.GetTemplate<TestItemTemplate>("TestItem+A");

            _provider.Dispose();

            Assert.That(item == null, Is.True);
            Assert.That(_provider.IsLoaded, Is.False);
        }

        [Test]
        public void LoadAsync_InvalidMapping_Throws()
        {
            _mapping.TypeGroups.Add(new ConfigTypeMapping.Group { TypeIdentifier = "Gone", TypeString = "Gone, Missing" });
            _provider = new ConfigProvider(_mapping, Array.Empty<ConfigLoader>(), _log.Logger);

            Assert.Throws<ConfigException>(() => Run(_provider.LoadAsync()));
        }

        private void Load(params ConfigLoader[] loaders)
        {
            _provider = new ConfigProvider(_mapping, new List<ConfigLoader>(loaders), _log.Logger);
            Run(_provider.LoadAsync());
        }

        /// <summary>Completes a load whose loaders all completed synchronously, rethrowing its exception.</summary>
        private static void Run(Task task)
        {
            Assert.That(task.IsCompleted, Is.True, "load completed synchronously");
            task.GetAwaiter().GetResult();
        }
    }
}
