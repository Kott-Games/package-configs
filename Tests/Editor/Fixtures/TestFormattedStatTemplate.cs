using Kott.Configs.Attributes;

namespace Kott.Configs.Tests
{
    [TemplateEnum(typeof(TestStat), IdFormat = "stats/{0}")]
    public class TestFormattedStatTemplate : Template
    { }
}
