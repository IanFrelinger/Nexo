using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Nexo.Shared.Models;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Feature.Container.Interfaces
{
    /// <summary>
    /// Defines the contract for managing and orchestrating containerized environments.
    /// </summary>
    public interface IContainerOrchestrator
    {
        ContainerRuntime Runtime { get; }
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<CommandResult> ExecuteInContainerAsync(string containerName, string[] command, CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default(CancellationToken));
        Task StartContainerAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
        Task StopContainerAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
        Task BuildContainerAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<string>> ListRunningContainersAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<string> GetContainerLogsAsync(string containerName, int lines = 100, CancellationToken cancellationToken = default(CancellationToken));
    }
}