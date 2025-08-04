using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Interfaces
{
    /// <summary>
    /// Interface for platform-specific performance optimization.
    /// Part of Epic 6.2: Platform-Specific Feature Integration, Story 6.2.3: Performance Optimization.
    /// </summary>
    public interface IPerformanceOptimization
    {
        /// <summary>
        /// Initializes performance optimization for a specific platform.
        /// </summary>
        /// <param name="platformType">The target platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Initialization result</returns>
        Task<PerformanceOptimizationInitializationResult> InitializeAsync(PlatformType platformType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies platform-specific performance tuning.
        /// </summary>
        /// <param name="tuningProfile">The performance tuning profile to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance tuning result</returns>
        Task<PerformanceTuningResult> ApplyPerformanceTuningAsync(PerformanceTuningProfile tuningProfile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes memory usage for the current platform.
        /// </summary>
        /// <param name="optimizationStrategy">The memory optimization strategy</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Memory optimization result</returns>
        Task<MemoryOptimizationResult> OptimizeMemoryAsync(MemoryOptimizationStrategy optimizationStrategy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies battery life optimization strategies.
        /// </summary>
        /// <param name="batteryStrategy">The battery optimization strategy</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Battery optimization result</returns>
        Task<BatteryOptimizationResult> OptimizeBatteryLifeAsync(BatteryOptimizationStrategy batteryStrategy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts performance monitoring for the application.
        /// </summary>
        /// <param name="monitoringConfig">The monitoring configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance monitoring result</returns>
        Task<PerformanceMonitoringResult> StartPerformanceMonitoringAsync(PerformanceMonitoringConfig monitoringConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops performance monitoring.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Monitoring stop result</returns>
        Task<PerformanceMonitoringResult> StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current performance metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance metrics result</returns>
        Task<PerformanceMetricsResult> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes performance bottlenecks and provides recommendations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance analysis result</returns>
        Task<PerformanceAnalysisResult> AnalyzePerformanceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets platform-specific performance recommendations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance recommendations result</returns>
        Task<PerformanceRecommendationsResult> GetPerformanceRecommendationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies automatic performance optimizations based on current conditions.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Automatic optimization result</returns>
        Task<AutomaticOptimizationResult> ApplyAutomaticOptimizationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates performance optimization settings.
        /// </summary>
        /// <param name="settings">The settings to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<PerformanceValidationResult> ValidateOptimizationSettingsAsync(PerformanceOptimizationSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets performance optimization to default settings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Reset result</returns>
        Task<PerformanceResetResult> ResetToDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disposes of performance optimization resources.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposal result</returns>
        Task<PerformanceDisposalResult> DisposeAsync(CancellationToken cancellationToken = default);
    }
} 