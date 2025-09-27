using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for platform-specific performance optimization service.
    /// Implements platform-specific performance tuning, memory, and battery life optimization.
    /// </summary>
    public partial interface IPlatformPerformanceOptimizationService
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

    // Platform-specific models for Platform Performance Optimization
    public partial class PerformanceOptimizationOptions
    {
        public string Platform { get; set; } = string.Empty;
        public bool OptimizeMemory { get; set; } = true;
        public bool OptimizeCPU { get; set; } = true;
        public bool OptimizeGPU { get; set; } = false;
        public bool OptimizeNetwork { get; set; } = true;
        public bool OptimizeBattery { get; set; } = true;
        public bool OptimizeStorage { get; set; } = true;
        public bool OptimizeConsistency { get; set; } = true;
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    public partial class PlatformPerformanceOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public List<string> OptimizationsApplied { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public partial class MemoryOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double MemoryReduction { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public partial class CPUOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double PerformanceImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public partial class GPUOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double RenderingImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public partial class NetworkOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double LatencyReduction { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public partial class BatteryOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double BatteryLifeImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public partial class StorageOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double StorageReduction { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public partial class ConsistencyOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double ConsistencyImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    // Additional Platform Performance Optimization models
    public partial class MemoryOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public partial class CPUOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public partial class GPUOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public partial class NetworkOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public partial class BatteryOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public partial class StorageOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public partial class ConsistencyOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
