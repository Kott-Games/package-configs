using Kott.Configs.Attributes;

namespace Kott.Configs.Tests
{
    public enum TestStat
    {
        Health,
        Mana,
    }

    [TemplateEnum(typeof(TestStat))]
    public class TestStatTemplate : Template
    {
        public float Value;
    }
}
