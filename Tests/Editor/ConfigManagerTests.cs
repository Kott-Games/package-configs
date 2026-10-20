#if KOTT_CONFIGS_SCOPES
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using static Kott.Configs.Tests.TestPayloads;
using Object = UnityEngine.Object;

namespace Kott.Configs.Tests
{
    public class ConfigManagerTests
    {
        private readonly List<Object> _objects = new();
        private ConfigManager _manager;

        [SetUp]
        public void SetUp()
        {
            ConfigTypeMapping mapping = Mapping();
            _objects.Add(mapping);
            _manager = new ConfigManager { TypeMapping = mapping };
        }

        [TearDown]
        public void TearDown()
        {
            Run(_manager.Uninitialize(null));
            foreach (Object target in _objects)
            {
                Object.DestroyImmediate(target);
            }

            _objects.Clear();
        }

        [Test]
        public void Initialize_TextAssetLoader_ProvidesTemplates()
        {
            _manager.Loaders.Add(new TextAssetLoader(Asset(Payload(Item("TestItem+A", 3)))));

            Run(_manager.Initialize(null));

            Assert.That(_manager.GetTemplate<TestItemTemplate>("TestItem+A").Damage, Is.EqualTo(3));
            Assert.That(_manager.GetCollection<TestItemTemplate>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void Initialize_RequiredLoaderFails_Throws()
        {
            _manager.Loaders.Add(FakeLoader.Failing(new IOException("offline")));

            Assert.Throws<ConfigException>(() => Run(_manager.Initialize(null)));
        }

        [Test]
        public void Uninitialize_Loaded_DestroysTemplates()
        {
            _manager.Loaders.Add(new FakeLoader(Payload(Item("TestItem+A"))));
            Run(_manager.Initialize(null));
            TestItemTemplate item = _manager.GetTemplate<TestItemTemplate>("TestItem+A");

            Run(_manager.Uninitialize(null));

            Assert.That(item == null, Is.True);
            Assert.Throws<InvalidOperationException>(() => _manager.GetTemplate<TestItemTemplate>("TestItem+A"));
        }

        private TextAsset Asset(string text)
        {
            TextAsset asset = new(text);
            _objects.Add(asset);
            return asset;
        }

        /// <summary>Runs a routine whose waits are already complete, including nested routines.</summary>
        private static void Run(IEnumerator routine)
        {
            Stack<IEnumerator> stack = new();
            stack.Push(routine);
            for (int steps = 0; stack.Count > 0; steps++)
            {
                Assert.That(steps, Is.LessThan(1000), "routine completes without waiting for frames");
                IEnumerator current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                }
                else if (current.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                }
            }
        }
    }
}
#endif
