using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Base interface for all plugins in the Nexo system.
    /// </summary>
    // Merged from IPluginManager
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Description { get; }
        string Author { get; }
        bool IsEnabled { get; }
        Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
        Task ShutdownAsync(CancellationToken cancellationToken = default);
        Task<PluginResult> ExecuteAsync(string[] args);
    }
}
