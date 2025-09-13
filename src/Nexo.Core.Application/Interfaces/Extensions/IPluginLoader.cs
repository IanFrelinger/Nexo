using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Extensions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    public interface IPluginLoader
    {
        Task<PluginLoadResult> LoadPluginAsync(byte[] assemblyBytes, string pluginName);
        Task<PluginLoadResult> LoadPluginAsync(string assemblyPath, string pluginName);
        Task<bool> UnloadPluginAsync(string pluginName);
        Task<IPlugin?> GetPluginAsync(string pluginName);
        Task<bool> IsPluginLoadedAsync(string pluginName);
    }
}
