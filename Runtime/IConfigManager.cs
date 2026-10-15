using System.Collections.Generic;
using Kott.Scopes;

namespace Kott.Configs
{
    public interface IConfigManager : ISingletonContract<IConfigManager>
    {
        IReadOnlyList<T> GetCollection<T>() where T : Template;
        T GetTemplate<T>(string key) where T : Template;
    }
}
