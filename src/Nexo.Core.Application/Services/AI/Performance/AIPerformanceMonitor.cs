using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Performance
{
    /// <summary>
    /// Real AI performance monitoring service for tracking and optimizing AI operations
    /// </summary>
    public class AIPerformanceMonitor
    {
        private readonly ILogger<AIPerformanceMonitor> _logger;
        private readonly Dictionary<string, PerformanceMetrics> _activeOperations;
        private readonly List<PerformanceMetrics> _historicalMetrics;
        private readonly object _lockObject = new object();

        public AIPerformanceMonitor(ILogger<AIPerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeOperations = new Dictionary<string, PerformanceMetrics>();
            _historicalMetrics = new List<PerformanceMetrics>();
        }

        public async Task<PerformanceMetrics> StartOperationAsync(string operationId, AIOperationType operationType, AIProviderType providerType, AIEngineType engineType)
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
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start performance monitoring for operation {OperationId}", operationId);
                throw;
            }
        }

        public async Task<PerformanceMetrics> EndOperationAsync(string operationId, bool success, string? errorMessage = null)
        {
            try
            {
                _logger.LogDebug("Ending performance monitoring for operation {OperationId}", operationId);

                PerformanceMetrics metrics;
                lock (_lockObject)
                {
                    if (!_activeOperations.TryGetValue(operationId, out metrics))
                    {
                        _logger.LogWarning("Operation {OperationId} not found in active operations", operationId);
                        return null;
                    }
                    
                    _activeOperations.Remove(operationId);
                }

                // Update metrics
                metrics.EndTime = DateTime.UtcNow;
                metrics.Duration = metrics.EndTime - metrics.StartTime;
                metrics.Status = success ? AIOperationStatus.Completed : AIOperationStatus.Failed;
                metrics.ErrorMessage = errorMessage;

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

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end performance monitoring for operation {OperationId}", operationId);
                throw;
            }
        }

        public async Task<PerformanceMetrics> GetOperationMetricsAsync(string operationId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_activeOperations.TryGetValue(operationId, out var activeMetrics))
                    {
                        return activeMetrics;
                    }
                }

                // Check historical metrics
                lock (_lockObject)
                {
                    foreach (var metrics in _historicalMetrics)
                    {
                        if (metrics.OperationId == operationId)
                        {
                            return metrics;
                        }
                    }
                }

                _logger.LogWarning("Operation {OperationId} not found in metrics", operationId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get operation metrics for {OperationId}", operationId);
                throw;
            }
        }

        public async Task<List<PerformanceMetrics>> GetHistoricalMetricsAsync(TimeSpan? timeRange = null, AIOperationType? operationType = null, AIProviderType? providerType = null)
        {
            try
            {
                var cutoffTime = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.MinValue;
                
                lock (_lockObject)
                {
                    var filteredMetrics = new List<PerformanceMetrics>();
                    
                    foreach (var metrics in _historicalMetrics)
                    {
                        if (metrics.StartTime < cutoffTime)
                            continue;
                            
                        if (operationType.HasValue && metrics.OperationType != operationType.Value)
                            continue;
                            
                        if (providerType.HasValue && metrics.ProviderType != providerType.Value)
                            continue;
                            
                        filteredMetrics.Add(metrics);
                    }
                    
                    return filteredMetrics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get historical metrics");
                throw;
            }
        }

        public async Task<PerformanceStatistics> GetPerformanceStatisticsAsync(TimeSpan? timeRange = null)
        {
            try
            {
                _logger.LogInformation("Calculating performance statistics");

                var metrics = await GetHistoricalMetricsAsync(timeRange);
                
                if (metrics.Count == 0)
                {
                    return new PerformanceStatistics
                    {
                        TotalOperations = 0,
                        AverageDuration = TimeSpan.Zero,
                        AveragePerformanceScore = 0,
                        SuccessRate = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                var statistics = new PerformanceStatistics
                {
                    TotalOperations = metrics.Count,
                    SuccessfulOperations = metrics.Count(m => m.Status == AIOperationStatus.Completed),
                    FailedOperations = metrics.Count(m => m.Status == AIOperationStatus.Failed),
                    AverageDuration = TimeSpan.FromMilliseconds(metrics.Average(m => m.Duration.TotalMilliseconds)),
                    MinDuration = metrics.Min(m => m.Duration),
                    MaxDuration = metrics.Max(m => m.Duration),
                    AveragePerformanceScore = metrics.Average(m => m.PerformanceScore),
                    AverageMemoryUsage = metrics.Average(m => m.FinalMemoryUsage),
                    AverageCpuUsage = metrics.Average(m => m.FinalCpuUsage),
                    LastUpdated = DateTime.UtcNow
                };

                // Calculate success rate
                statistics.SuccessRate = (double)statistics.SuccessfulOperations / statistics.TotalOperations * 100;

                // Calculate performance trends
                statistics.PerformanceTrend = CalculatePerformanceTrend(metrics);

                _logger.LogInformation("Performance statistics calculated: {TotalOperations} operations, {SuccessRate}% success rate, {AverageDuration}ms average duration", 
                    statistics.TotalOperations, statistics.SuccessRate, statistics.AverageDuration.TotalMilliseconds);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate performance statistics");
                throw;
            }
        }

        public async Task<List<PerformanceRecommendation>> GetPerformanceRecommendationsAsync()
        {
            try
            {
                _logger.LogInformation("Generating performance recommendations");

                var recommendations = new List<PerformanceRecommendation>();
                var statistics = await GetPerformanceStatisticsAsync();

                // Check for performance issues
                if (statistics.AveragePerformanceScore < 70)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Performance Score",
                        Priority = "High",
                        Message = "Average performance score is below 70. Consider optimizing AI operations.",
                        Recommendation = "Review and optimize AI model configurations, increase memory allocation, or consider using a different AI provider.",
                        Impact = "High"
                    });
                }

                if (statistics.SuccessRate < 95)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Success Rate",
                        Priority = "High",
                        Message = $"Success rate is {statistics.SuccessRate:F1}%. Consider investigating failures.",
                        Recommendation = "Review error logs, check AI provider availability, and implement better error handling.",
                        Impact = "High"
                    });
                }

                if (statistics.AverageDuration.TotalSeconds > 30)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Response Time",
                        Priority = "Medium",
                        Message = $"Average response time is {statistics.AverageDuration.TotalSeconds:F1} seconds. Consider optimization.",
                        Recommendation = "Use smaller models, implement caching, or consider using faster AI providers.",
                        Impact = "Medium"
                    });
                }

                if (statistics.AverageMemoryUsage > 1024 * 1024 * 1024) // 1GB
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Memory Usage",
                        Priority = "Medium",
                        Message = "High memory usage detected. Consider memory optimization.",
                        Recommendation = "Use quantized models, implement memory pooling, or consider using smaller models.",
                        Impact = "Medium"
                    });
                }

                _logger.LogInformation("Generated {Count} performance recommendations", recommendations.Count);
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate performance recommendations");
                throw;
            }
        }

        private double GetCurrentCpuUsage()
        {
            try
            {
                // In a real implementation, this would get actual CPU usage
                // For now, return a mock value
                return 25.0; // 25% CPU usage
            }
            catch
            {
                return 0.0;
            }
        }

        private double CalculatePerformanceScore(PerformanceMetrics metrics)
        {
            try
            {
                var score = 100.0;

                // Deduct points for long duration
                if (metrics.Duration.TotalSeconds > 10)
                {
                    score -= Math.Min(30, (metrics.Duration.TotalSeconds - 10) * 2);
                }

                // Deduct points for high memory usage
                if (metrics.MemoryDelta > 100 * 1024 * 1024) // 100MB
                {
                    score -= Math.Min(20, (metrics.MemoryDelta - 100 * 1024 * 1024) / (10 * 1024 * 1024));
                }

                // Deduct points for high CPU usage
                if (metrics.CpuDelta > 50)
                {
                    score -= Math.Min(15, (metrics.CpuDelta - 50) / 5);
                }

                // Deduct points for failures
                if (metrics.Status == AIOperationStatus.Failed)
                {
                    score -= 50;
                }

                return Math.Max(0, Math.Min(100, score));
            }
            catch
            {
                return 0.0;
            }
        }

        private string CalculatePerformanceTrend(List<PerformanceMetrics> metrics)
        {
            try
            {
                if (metrics.Count < 10)
                    return "Insufficient Data";

                // Get recent metrics (last 20% of data)
                var recentCount = Math.Max(5, metrics.Count / 5);
                var recentMetrics = metrics.TakeLast(recentCount).ToList();
                var olderMetrics = metrics.Take(metrics.Count - recentCount).ToList();

                if (olderMetrics.Count == 0)
                    return "Insufficient Data";

                var recentAverage = recentMetrics.Average(m => m.PerformanceScore);
                var olderAverage = olderMetrics.Average(m => m.PerformanceScore);

                var difference = recentAverage - olderAverage;

                if (difference > 5)
                    return "Improving";
                else if (difference < -5)
                    return "Declining";
                else
                    return "Stable";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    /// <summary>
    /// Performance metrics for AI operations
    /// </summary>
    public class PerformanceMetrics
    {
        public string OperationId { get; set; }
        public AIOperationType OperationType { get; set; }
        public AIProviderType ProviderType { get; set; }
        public AIEngineType EngineType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public AIOperationStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public long InitialMemoryUsage { get; set; }
        public long FinalMemoryUsage { get; set; }
        public long MemoryDelta { get; set; }
        public double InitialCpuUsage { get; set; }
        public double FinalCpuUsage { get; set; }
        public double CpuDelta { get; set; }
        public double PerformanceScore { get; set; }
    }

    /// <summary>
    /// Performance statistics for AI operations
    /// </summary>
    public class PerformanceStatistics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public double AveragePerformanceScore { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double AverageCpuUsage { get; set; }
        public string PerformanceTrend { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Performance recommendation for AI operations
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Type { get; set; }
        public string Priority { get; set; }
        public string Message { get; set; }
        public string Recommendation { get; set; }
        public string Impact { get; set; }
    }
}
