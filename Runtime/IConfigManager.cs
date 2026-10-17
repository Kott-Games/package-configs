using System.Collections.Generic;
using Kott.Scopes;

namespace Kott.Configs
{
    /// <summary>Runtime access to config templates. A manager contract: inject it with <c>[Inject]</c>.</summary>
    public interface IConfigManager : IManagerContract
    {
        IReadOnlyList<T> GetCollection<T>() where T : Template;
        T GetTemplate<T>(string key) where T : Template;
    }
}
