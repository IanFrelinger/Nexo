using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Platform;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for platform-specific performance optimization service.
    /// Implements platform-specific performance tuning, memory, and battery life optimization.
    /// </summary>
    public interface IPlatformPerformanceOptimizationService
    {
        /// <summary>
        /// Optimizes performance for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Platform performance optimization result</returns>
        Task<PlatformPerformanceOptimizationResult> OptimizePerformanceAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes memory usage for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Memory optimization result</returns>
        Task<MemoryOptimizationResult> OptimizeMemoryAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes CPU usage for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>CPU optimization result</returns>
        Task<CPUOptimizationResult> OptimizeCPUAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes GPU usage for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>GPU optimization result</returns>
        Task<GPUOptimizationResult> OptimizeGPUAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes network usage for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Network optimization result</returns>
        Task<NetworkOptimizationResult> OptimizeNetworkAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes battery usage for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Battery optimization result</returns>
        Task<BatteryOptimizationResult> OptimizeBatteryAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes storage usage for a specific platform.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage optimization result</returns>
        Task<StorageOptimizationResult> OptimizeStorageAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes cross-platform consistency.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="options">Performance optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Consistency optimization result</returns>
        Task<ConsistencyOptimizationResult> OptimizeConsistencyAsync(
            string platform,
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);
    }
}
