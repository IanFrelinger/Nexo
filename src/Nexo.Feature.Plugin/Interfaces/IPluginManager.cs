using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Manages the lifecycle and registration of plugins.
    /// </summary>
    public interface IPluginManager
    {
        Task LoadPluginsAsync(CancellationToken cancellationToken = default(CancellationToken));
        IEnumerable<IPlugin> GetPlugins();
        IEnumerable<T> GetPlugins<T>() where T : IPlugin;
        T GetPlugin<T>(string name) where T : IPlugin;
        Task RegisterPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default(CancellationToken));
        Task UnregisterPluginAsync(string pluginName, CancellationToken cancellationToken = default(CancellationToken));
        Task EnablePluginAsync(string pluginName, CancellationToken cancellationToken = default(CancellationToken));
        Task DisablePluginAsync(string pluginName, CancellationToken cancellationToken = default(CancellationToken));
        Task ReloadPluginsAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task ShutdownAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}