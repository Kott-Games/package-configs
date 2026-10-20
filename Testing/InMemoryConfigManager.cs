#if KOTT_CONFIGS_SCOPES
namespace Kott.Configs.Testing
{
    /// <summary>
    ///     <see cref="InMemoryConfigProvider" /> as <see cref="IConfigManager" />, to inject into code under test with
    ///     <c>TestInjector.Add&lt;IConfigManager&gt;(…)</c>.
    /// </summary>
    public sealed class InMemoryConfigManager : InMemoryConfigProvider, IConfigManager
    { }
}
#endif
