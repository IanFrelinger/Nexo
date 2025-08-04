using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for loading plugins.
    /// </summary>
    public interface IPluginLoader
    {
        Task<IEnumerable<IPlugin>> LoadPluginsAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<T> LoadPluginAsync<T>(string pluginName, CancellationToken cancellationToken = default(CancellationToken)) where T : IPlugin;
        string GetPluginDirectory();
        Task<bool> PluginExistsAsync(string pluginName, CancellationToken cancellationToken = default(CancellationToken));
    }
}

