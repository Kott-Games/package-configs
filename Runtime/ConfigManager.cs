using System.Collections.Generic;
using Kott.Scopes;

namespace Kott.Configs
{
    public abstract class ConfigManager : Manager<ConfigManager>
    {
        public abstract IReadOnlyList<T> GetCollection<T>() where T : Template;
        public abstract T GetTemplate<T>(string key) where T : Template;
    }
}