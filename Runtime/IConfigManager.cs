using Kott.Scopes;
using System.Collections.Generic;

namespace Kott.Configs
{
    public interface IConfigManager : IManagerContract<IConfigManager>
    {
        IReadOnlyList<T> GetCollection<T>() where T : Template;
        T GetTemplate<T>(string key) where T : Template;
    }
}