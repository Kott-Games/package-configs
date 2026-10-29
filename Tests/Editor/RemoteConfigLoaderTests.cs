#if KOTT_CONFIGS_REMOTE_CONFIG
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Kott.Configs.Tests
{
    public class RemoteConfigLoaderTests
    {
        [Test]
        public void EffectiveKey_NoKeySet_IsVersionedKey()
        {
            // regression: the versioned key was written, but the unversioned base key was read
            RemoteConfigLoader loader = new();

            Assert.That(loader.EffectiveKey, Is.EqualTo(ConfigVersionUtility.GetTemplateDataKey()));
            Assert.That(loader.EffectiveKey, Is.Not.EqualTo(ConfigVersionUtility.TemplateDataKeyBase));
        }

        [Test]
        public void EffectiveKey_KeySet_IsKey()
        {
            RemoteConfigLoader loader = new() { Key = "custom" };

            Assert.That(loader.EffectiveKey, Is.EqualTo("custom"));
        }

        [Test]
        public void LoadAsync_FetchesEffectiveKey()
        {
            RecordingRemoteConfigLoader loader = new();

            Task<string> load = loader.LoadAsync(null, CancellationToken.None);

            Assert.That(load.GetAwaiter().GetResult(), Is.EqualTo("[]"));
            Assert.That(loader.FetchedKey, Is.EqualTo(ConfigVersionUtility.GetTemplateDataKey()));
        }

        private sealed class RecordingRemoteConfigLoader : RemoteConfigLoader
        {
            public string FetchedKey { get; private set; }

            internal override Task<string> FetchJsonAsync(string key, CancellationToken cancellationToken)
            {
                FetchedKey = key;
                return Task.FromResult("[]");
            }
        }
    }
}
#endif
