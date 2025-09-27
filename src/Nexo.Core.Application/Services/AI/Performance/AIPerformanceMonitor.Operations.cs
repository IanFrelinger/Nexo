using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Performance
{
    /// <summary>
    /// AI operation monitoring functionality for AIPerformanceMonitor.
    /// </summary>
    public partial class AIPerformanceMonitor
    {
        /// <summary>
        /// Starts performance monitoring for an AI operation.
        /// </summary>
        private async Task<PerformanceMetrics> StartOperationInternalAsync(string operationId, AIOperationType operationType, AIProviderType providerType, AIEngineType engineType)
        {
            try
            {
                _logger.LogDebug("Starting performance monitoring for operation {OperationId}", operationId);

                var metrics = new PerformanceMetrics
                {
                    OperationId = operationId,
                    OperationType = operationType,
                    ProviderType = providerType,
                    EngineType = engineType,
                    StartTime = DateTime.UtcNow,
                    Status = AIOperationStatus.Running
                };

                // Capture initial memory usage
                metrics.InitialMemoryUsage = GC.GetTotalMemory(false);
                metrics.InitialCpuUsage = GetCurrentCpuUsage();

                lock (_lockObject)
                {
                    _activeOperations[operationId] = metrics;
                }

                _logger.LogDebug("Performance monitoring started for operation {OperationId}", operationId);
                return await Task.FromResult(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start performance monitoring for operation {OperationId}", operationId);
                throw;
            }
        }

        /// <summary>
        /// Ends performance monitoring for an AI operation.
        /// </summary>
        private async Task<PerformanceMetrics> EndOperationInternalAsync(string operationId, bool success, string? errorMessage = null)
        {
            try
            {
                _logger.LogDebug("Ending performance monitoring for operation {OperationId}", operationId);

                PerformanceMetrics? metrics;
                lock (_lockObject)
                {
                    if (!_activeOperations.TryGetValue(operationId, out metrics))
                    {
                        _logger.LogWarning("Operation {OperationId} not found in active operations", operationId);
                        return await Task.FromResult(new PerformanceMetrics { OperationId = operationId });
                    }
                    
                    _activeOperations.Remove(operationId);
                }

                if (metrics == null)
                {
                    _logger.LogWarning("Operation {OperationId} not found in active operations", operationId);
                    return await Task.FromResult(new PerformanceMetrics { OperationId = operationId });
                }

                // Update metrics
                metrics.EndTime = DateTime.UtcNow;
                metrics.Duration = metrics.EndTime - metrics.StartTime;
                metrics.Status = success ? AIOperationStatus.Completed : AIOperationStatus.Failed;
                metrics.ErrorMessage = errorMessage ?? string.Empty;

                // Capture final memory usage
                metrics.FinalMemoryUsage = GC.GetTotalMemory(false);
                metrics.MemoryDelta = metrics.FinalMemoryUsage - metrics.InitialMemoryUsage;
                metrics.FinalCpuUsage = GetCurrentCpuUsage();
                metrics.CpuDelta = metrics.FinalCpuUsage - metrics.InitialCpuUsage;

                // Calculate performance score
                metrics.PerformanceScore = CalculatePerformanceScore(metrics);

                // Add to historical metrics
                lock (_lockObject)
                {
                    _historicalMetrics.Add(metrics);
                    
                    // Keep only last 1000 metrics to prevent memory issues
                    if (_historicalMetrics.Count > 1000)
                    {
                        _historicalMetrics.RemoveAt(0);
                    }
                }

                _logger.LogInformation("Operation {OperationId} completed in {Duration}ms with performance score {Score}", 
                    operationId, metrics.Duration.TotalMilliseconds, metrics.PerformanceScore);

                return await Task.FromResult(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end performance monitoring for operation {OperationId}", operationId);
                throw;
            }
        }

        /// <summary>
        /// Gets performance metrics for a specific operation.
        /// </summary>
        private async Task<PerformanceMetrics> GetOperationMetricsInternalAsync(string operationId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_activeOperations.TryGetValue(operationId, out var activeMetrics))
                    {
                        return await Task.FromResult(activeMetrics);
                    }
                }

                // Check historical metrics
                lock (_lockObject)
                {
                    foreach (var metrics in _historicalMetrics)
                    {
                        if (metrics.OperationId == operationId)
                        {
                            return await Task.FromResult(metrics);
                        }
                    }
                }

                _logger.LogWarning("Operation {OperationId} not found in metrics", operationId);
                return await Task.FromResult(new PerformanceMetrics { OperationId = operationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get operation metrics for {OperationId}", operationId);
                throw;
            }
        }
    }
}
