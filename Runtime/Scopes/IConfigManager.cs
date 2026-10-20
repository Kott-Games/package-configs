using Kott.Scopes;

namespace Kott.Configs
{
    /// <summary>
    ///     Config access as a Scopes manager contract: inject it with <c>[Inject]</c>. Provided by
    ///     <see cref="ConfigManager" />.
    /// </summary>
    public interface IConfigManager : IConfigProvider, IManagerContract
    { }
}
