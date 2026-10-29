using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kott.Configs.Tests
{
    public class LoaderTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "KottConfigsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        [Test]
        public void TextAssetLoader_Payload_ReturnsText()
        {
            TextAsset asset = new("[]");
            try
            {
                Assert.That(Load(new TextAssetLoader(asset)), Is.EqualTo("[]"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void TextAssetLoader_NoPayload_Throws()
        {
            Assert.Throws<ConfigException>(() => Load(new TextAssetLoader()));
        }

        [Test]
        public void FileLoader_ExistingFile_ReturnsText()
        {
            string path = Path.Combine(_directory, "config.json");
            File.WriteAllText(path, "[1]");

            Assert.That(Load(new FileLoader(FileLoader.PathRoot.Absolute, path)), Is.EqualTo("[1]"));
        }

        [Test]
        public void FileLoader_MissingFile_ThrowsNamingPath()
        {
            string path = Path.Combine(_directory, "missing.json");

            FileNotFoundException exception =
                Assert.Throws<FileNotFoundException>(() => Load(new FileLoader(FileLoader.PathRoot.Absolute, path)));

            Assert.That(exception.Message, Does.Contain("missing.json"));
        }

        [Test]
        public void FileLoader_MissingFileSkipped_ReturnsNull()
        {
            string path = Path.Combine(_directory, "missing.json");

            Assert.That(Load(new FileLoader(FileLoader.PathRoot.Absolute, path, true)), Is.Null);
        }

        [Test]
        public void FileLoader_PersistentDataRoot_CombinesPath()
        {
            FileLoader loader = new(FileLoader.PathRoot.PersistentData, "overrides.json");

            Assert.That(loader.FullPath, Is.EqualTo(Path.Combine(Application.persistentDataPath, "overrides.json")));
        }

        [Test]
        public void Timeout_Negative_ClampedToZero()
        {
            TextAssetLoader loader = new() { Timeout = -1f };

            Assert.That(loader.Timeout, Is.Zero);
        }

        private static string Load(ConfigLoader loader)
        {
            Task<string> task = loader.LoadAsync(null, CancellationToken.None);
            Assert.That(task.IsCompleted, Is.True, "load completed synchronously");
            return task.GetAwaiter().GetResult();
        }
    }
}
