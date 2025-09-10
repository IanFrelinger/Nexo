using System;
using System.Threading;
using System.Threading.Tasks;

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

    // Platform-specific models for Platform Performance Optimization
    public class PerformanceOptimizationOptions
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

    public class PlatformPerformanceOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public List<string> OptimizationsApplied { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public class MemoryOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double MemoryReduction { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public class CPUOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double PerformanceImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public class GPUOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double RenderingImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public class NetworkOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double LatencyReduction { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public class BatteryOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double BatteryLifeImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public class StorageOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double StorageReduction { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    public class ConsistencyOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double ConsistencyImprovement { get; set; }
        public List<string> Optimizations { get; set; } = new();
    }

    // Additional Platform Performance Optimization models
    public class MemoryOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class CPUOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class GPUOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class NetworkOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class BatteryOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class StorageOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class ConsistencyOptimizationStrategy
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Techniques { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
