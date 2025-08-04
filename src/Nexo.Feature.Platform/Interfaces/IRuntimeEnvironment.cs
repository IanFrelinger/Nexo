using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Nexo.Core.Application.Models;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Provides runtime environment abstraction for executing code in different languages and environments.
    /// </summary>
    public interface IRuntimeEnvironment
    {
        string RuntimeName { get; }
        string Version { get; }
        string[] SupportedExtensions { get; }
        bool IsAvailable { get; }
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<RuntimeExecutionResult> ExecuteAsync(RuntimeExecutionRequest request, CancellationToken cancellationToken = default(CancellationToken));
        Task<RuntimeExecutionResult> ExecuteFileAsync(string filePath, string[] arguments, CancellationToken cancellationToken = default(CancellationToken));
        Task<RuntimeValidationResult> ValidateAsync(string code, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<RuntimePackage>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<RuntimePackageResult> InstallPackageAsync(string packageName, string version, CancellationToken cancellationToken = default(CancellationToken));
    }
}