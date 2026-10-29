#if KOTT_CONFIGS_DEPLOYMENT
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Kott.Configs.Tests
{
    public class RemoteConfigPublishTargetTests
    {
        private static readonly JObject Payload = JObject.Parse(TestPayloads.Payload(TestPayloads.Item("TestItem+A")));

        [Test]
        public void Write_NoFile_CreatesFileWithSchemaAndEntry()
        {
            JObject file = JObject.Parse(RemoteConfigPublishTarget.Write(null, "configTemplatesX", Payload));

            Assert.That(file["$schema"].Value<string>(), Is.EqualTo(RemoteConfigPublishTarget.Schema));
            Assert.That(JToken.DeepEquals(file["entries"]["configTemplatesX"], Payload), Is.True);
        }

        [Test]
        public void Write_ExistingFile_KeepsOtherEntriesAndReplacesKey()
        {
            string existing = new JObject
            {
                { "$schema", RemoteConfigPublishTarget.Schema },
                { "entries", new JObject { { "other", 3 }, { "configTemplatesX", "old" } } },
            }.ToString();

            JObject file = JObject.Parse(RemoteConfigPublishTarget.Write(existing, "configTemplatesX", Payload));

            Assert.That(file["entries"]["other"].Value<int>(), Is.EqualTo(3));
            Assert.That(JToken.DeepEquals(file["entries"]["configTemplatesX"], Payload), Is.True);
        }

        [Test]
        public void Write_InvalidFile_Throws()
        {
            Assert.Throws<ConfigException>(() => RemoteConfigPublishTarget.Write("{", "key", Payload));
        }

        [Test]
        public void EffectiveKey_NoKey_IsVersionedKeyOfLoader()
        {
            Assert.That(new RemoteConfigPublishTarget().EffectiveKey, Is.EqualTo(ConfigVersionUtility.GetTemplateDataKey()));
        }
    }
}
#endif
