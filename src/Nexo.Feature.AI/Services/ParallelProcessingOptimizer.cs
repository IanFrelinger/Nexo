using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Core.Application.Interfaces;
using Nexo.Shared.Models;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Parallel processing optimizer that provides intelligent parallel processing strategies and resource optimization.
    /// </summary>
    public class ParallelProcessingOptimizer : IParallelProcessingOptimizer
    {
        private readonly ILogger<ParallelProcessingOptimizer> _logger;
        private readonly IResourceManager _resourceManager;
        private readonly Nexo.Shared.Interfaces.Resource.IResourceMonitor _resourceMonitor;
        private readonly Dictionary<string, Nexo.Feature.AI.Interfaces.ProcessingMetrics> _processingMetrics;
        private readonly object _metricsLock = new object();

        public ParallelProcessingOptimizer(
            ILogger<ParallelProcessingOptimizer> logger,
            IResourceManager resourceManager,
            Nexo.Shared.Interfaces.Resource.IResourceMonitor resourceMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _processingMetrics = new Dictionary<string, ProcessingMetrics>();
        }

        public async Task<ProcessingStrategy> DetermineOptimalStrategyAsync(IEnumerable<ModelRequest> requests, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Determining optimal processing strategy for {Count} requests", requests.Count());

            try
            {
                var requestList = requests.ToList();
                var systemResourceUsage = await _resourceMonitor.GetResourceUsageAsync(cancellationToken);
                var resourceLimits = await _resourceManager.GetLimitsAsync(cancellationToken);
                
                // Convert SystemResourceUsage to ResourceUsage for compatibility
                var resourceUsage = new Nexo.Shared.Interfaces.Resource.ResourceUsage
                {
                    AllocatedByType = new Dictionary<ResourceType, long>
                    {
                        { ResourceType.CPU, (long)(systemResourceUsage.CpuUsagePercentage * 100) },
                        { ResourceType.Memory, systemResourceUsage.Memory.UsedBytes },
                        { ResourceType.Storage, systemResourceUsage.Disk.UsedBytes }
                    },
                    AvailableByType = new Dictionary<ResourceType, long>
                    {
                        { ResourceType.CPU, (long)((100 - systemResourceUsage.CpuUsagePercentage) * 100) },
                        { ResourceType.Memory, systemResourceUsage.Memory.AvailableBytes },
                        { ResourceType.Storage, systemResourceUsage.Disk.AvailableBytes }
                    },
                    UtilizationByType = new Dictionary<ResourceType, double>
                    {
                        { ResourceType.CPU, systemResourceUsage.CpuUsagePercentage },
                        { ResourceType.Memory, systemResourceUsage.Memory.UsagePercentage },
                        { ResourceType.Storage, systemResourceUsage.Disk.UsagePercentage }
                    },
                    ActiveAllocations = new List<Nexo.Shared.Interfaces.Resource.ResourceAllocation>(),
                    Timestamp = systemResourceUsage.Timestamp
                };

                // Analyze request characteristics
                var requestAnalysis = AnalyzeRequests(requestList);
                
                // Determine optimal parallelism based on resources and request characteristics
                var optimalParallelism = CalculateOptimalParallelism(requestAnalysis, resourceUsage, resourceLimits);
                
                // Determine processing order
                var processingOrder = DetermineProcessingOrder(requestList, requestAnalysis);
                
                // Create processing strategy
                var strategy = new ProcessingStrategy
                {
                    MaxParallelism = optimalParallelism,
                    ProcessingOrder = processingOrder,
                    BatchSize = CalculateOptimalBatchSize(requestAnalysis, optimalParallelism),
                    ResourceAllocation = DetermineResourceAllocation(requestAnalysis, resourceLimits),
                    EstimatedDuration = EstimateProcessingDuration(requestList, optimalParallelism, resourceUsage),
                    PriorityLevel = DeterminePriorityLevel(requestAnalysis)
                };

                _logger.LogInformation("Optimal strategy determined: {Parallelism} parallel, {BatchSize} batch size", 
                    strategy.MaxParallelism, strategy.BatchSize);

                return strategy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining optimal processing strategy");
                return GetDefaultStrategy();
            }
        }

        public async Task<IEnumerable<ModelResponse>> ProcessInParallelAsync(
            IEnumerable<ModelRequest> requests, 
            ProcessingStrategy strategy,
            Func<ModelRequest, CancellationToken, Task<ModelResponse>> processor,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing {Count} requests in parallel with strategy", requests.Count());

            try
            {
                var requestList = requests.ToList();
                var results = new List<ModelResponse>();
                var semaphore = new SemaphoreSlim(strategy.MaxParallelism, strategy.MaxParallelism);
                var tasks = new List<Task<ModelResponse>>();

                // Process requests in batches according to strategy
                var batches = requestList
                    .Select((request, index) => new { request, index })
                    .GroupBy(x => x.index / strategy.BatchSize)
                    .Select(g => g.Select(x => x.request).ToList())
                    .ToList();

                foreach (var batch in batches)
                {
                    var batchTasks = batch.Select(async request =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var startTime = DateTime.UtcNow;
                            var response = await processor(request, cancellationToken);
                            var duration = DateTime.UtcNow - startTime;
                            
                            // Record metrics
                            RecordProcessingMetrics(request, duration, response);
                            
                            return response;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    var batchResults = await Task.WhenAll(batchTasks);
                    results.AddRange(batchResults);

                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                }

                _logger.LogInformation("Parallel processing completed for {Count} requests", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in parallel processing");
                throw;
            }
        }

        public async Task<ProcessingOptimization> OptimizeProcessingAsync(IEnumerable<Nexo.Feature.AI.Interfaces.ProcessingMetrics> metrics, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Optimizing processing based on {Count} metrics", metrics.Count());

            try
            {
                var metricsList = metrics.ToList();
                var optimization = new ProcessingOptimization();

                // Analyze performance patterns
                var performanceAnalysis = AnalyzePerformancePatterns(metricsList);
                
                // Identify bottlenecks
                var bottlenecks = IdentifyBottlenecks(metricsList);
                
                // Generate optimization recommendations
                var recommendations = GenerateOptimizationRecommendations(performanceAnalysis, bottlenecks);
                
                // Update processing strategy
                optimization.RecommendedStrategy = UpdateStrategyBasedOnMetrics(performanceAnalysis);
                optimization.Recommendations = recommendations;
                optimization.ExpectedImprovement = CalculateExpectedImprovement(performanceAnalysis, recommendations);
                optimization.Bottlenecks = bottlenecks;

                _logger.LogInformation("Processing optimization completed with {Count} recommendations", recommendations.Count);
                return optimization;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing processing");
                return new ProcessingOptimization();
            }
        }

        public async Task<ProcessingPerformance> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting processing performance metrics");

            try
            {
                lock (_metricsLock)
                {
                    var metrics = _processingMetrics.Values.ToList();
                    
                    return new ProcessingPerformance
                    {
                        TotalRequestsProcessed = metrics.Count,
                        AverageProcessingTime = metrics.Any() ? metrics.Average(m => m.ProcessingTime.TotalMilliseconds) : 0,
                        AverageResponseSize = metrics.Any() ? metrics.Average(m => m.ResponseSize) : 0,
                        SuccessRate = metrics.Any() ? (double)metrics.Count(m => m.Success) / metrics.Count : 0,
                        ResourceUtilization = metrics.Any() ? metrics.Average(m => m.ResourceUtilization) : 0,
                        ProcessingTrends = AnalyzeProcessingTrends(metrics)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return new ProcessingPerformance();
            }
        }

        private RequestAnalysis AnalyzeRequests(List<ModelRequest> requests)
        {
            return new RequestAnalysis
            {
                TotalRequests = requests.Count,
                AverageInputLength = requests.Average(r => r.Input?.Length ?? 0),
                MaxInputLength = requests.Max(r => r.Input?.Length ?? 0),
                MinInputLength = requests.Min(r => r.Input?.Length ?? 0),
                RequestTypes = requests.GroupBy(r => {
                    if (r.Metadata != null && r.Metadata.ContainsKey("type"))
                        return r.Metadata["type"]?.ToString() ?? "unknown";
                    return "unknown";
                }).ToDictionary(g => g.Key, g => g.Count()),
                ComplexityScore = CalculateComplexityScore(requests)
            };
        }

        private int CalculateOptimalParallelism(RequestAnalysis analysis, Nexo.Shared.Interfaces.Resource.ResourceUsage resourceUsage, Nexo.Shared.Interfaces.Resource.ResourceLimits resourceLimits)
        {
            // Base parallelism on available CPU cores
            var baseParallelism = Environment.ProcessorCount;
            
            // Adjust based on resource usage
                            var cpuUtilization = resourceUsage.UtilizationByType.ContainsKey(Nexo.Shared.Interfaces.Resource.ResourceType.CPU) ? resourceUsage.UtilizationByType[Nexo.Shared.Interfaces.Resource.ResourceType.CPU] : 0;
                var memoryUtilization = resourceUsage.UtilizationByType.ContainsKey(Nexo.Shared.Interfaces.Resource.ResourceType.Memory) ? resourceUsage.UtilizationByType[Nexo.Shared.Interfaces.Resource.ResourceType.Memory] : 0;
            
            if (cpuUtilization > 80)
                baseParallelism = Math.Max(1, baseParallelism / 2);
            else if (cpuUtilization < 30)
                baseParallelism = Math.Min(baseParallelism * 2, baseParallelism * 2); // Use base parallelism as fallback
            
            // Adjust based on request complexity
            if (analysis.ComplexityScore > 0.7)
                baseParallelism = Math.Max(1, baseParallelism / 2);
            
            return Math.Max(1, baseParallelism);
        }

        private List<ModelRequest> DetermineProcessingOrder(List<ModelRequest> requests, RequestAnalysis analysis)
        {
            // Sort by priority: smaller requests first for better throughput
            return requests.OrderBy(r => r.Input?.Length ?? 0).ToList();
        }

        private int CalculateOptimalBatchSize(RequestAnalysis analysis, int parallelism)
        {
            // Batch size should be proportional to parallelism but not too large
            var baseBatchSize = Math.Max(1, analysis.TotalRequests / parallelism);
            return Math.Min(baseBatchSize, 10); // Cap at 10 to prevent memory issues
        }

        private Nexo.Feature.AI.Interfaces.ResourceAllocation DetermineResourceAllocation(RequestAnalysis analysis, Nexo.Shared.Interfaces.Resource.ResourceLimits resourceLimits)
        {
            return new Nexo.Feature.AI.Interfaces.ResourceAllocation
            {
                MaxCpuPercentage = Math.Min(80.0, resourceLimits.MaximumByType.ContainsKey(Nexo.Shared.Interfaces.Resource.ResourceType.CPU) ? resourceLimits.MaximumByType[Nexo.Shared.Interfaces.Resource.ResourceType.CPU] / 100.0 : 50.0),
                MaxMemoryBytes = resourceLimits.MaximumByType.ContainsKey(Nexo.Shared.Interfaces.Resource.ResourceType.Memory) ? resourceLimits.MaximumByType[Nexo.Shared.Interfaces.Resource.ResourceType.Memory] : 1024 * 1024 * 1024,
                MaxConcurrentRequests = Math.Min(10, analysis.TotalRequests),
                PriorityLevel = DeterminePriorityLevel(analysis)
            };
        }

        private TimeSpan EstimateProcessingDuration(List<ModelRequest> requests, int parallelism, Nexo.Shared.Interfaces.Resource.ResourceUsage resourceUsage)
        {
            // Simple estimation based on average processing time and parallelism
            var averageProcessingTime = TimeSpan.FromMilliseconds(1000); // Default estimate
            var estimatedTime = TimeSpan.FromMilliseconds((requests.Count * averageProcessingTime.TotalMilliseconds) / parallelism);
            
            // Adjust based on resource usage
            var cpuUtilization = resourceUsage.UtilizationByType.ContainsKey(Nexo.Shared.Interfaces.Resource.ResourceType.CPU) ? resourceUsage.UtilizationByType[Nexo.Shared.Interfaces.Resource.ResourceType.CPU] : 0;
            if (cpuUtilization > 70)
                estimatedTime = TimeSpan.FromMilliseconds(estimatedTime.TotalMilliseconds * 1.5);
            
            return estimatedTime;
        }

        private PriorityLevel DeterminePriorityLevel(RequestAnalysis analysis)
        {
            if (analysis.ComplexityScore > 0.8)
                return PriorityLevel.High;
            if (analysis.ComplexityScore > 0.5)
                return PriorityLevel.Normal;
            return PriorityLevel.Low;
        }

        private double CalculateComplexityScore(List<ModelRequest> requests)
        {
            if (!requests.Any()) return 0.0;
            
            var avgLength = requests.Average(r => r.Input?.Length ?? 0);
            var maxTokens = requests.Max(r => r.MaxTokens);
            var hasMetadata = requests.Count(r => r.Metadata?.Any() == true);
            
            var lengthScore = Math.Min(avgLength / 1000.0, 1.0);
            var tokenScore = Math.Min(maxTokens / 4000.0, 1.0);
            var metadataScore = (double)hasMetadata / requests.Count;
            
            return (lengthScore + tokenScore + metadataScore) / 3.0;
        }

        private void RecordProcessingMetrics(ModelRequest request, TimeSpan duration, ModelResponse response)
        {
            lock (_metricsLock)
            {
                var key = $"{request.Input?.GetHashCode()}_{request.MaxTokens}";
                
                // Handle metadata safely for older .NET versions
                string requestType = "unknown";
                if (request.Metadata != null && request.Metadata.ContainsKey("type"))
                {
                    requestType = request.Metadata["type"]?.ToString() ?? "unknown";
                }
                
                _processingMetrics[key] = new Nexo.Feature.AI.Interfaces.ProcessingMetrics
                {
                    RequestType = requestType,
                    ProcessingTime = duration,
                    ResponseSize = response.Content?.Length ?? 0,
                    Success = response.Content != null, // Assume success if content is not null
                    ResourceUtilization = 0.5, // Placeholder
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private PerformanceAnalysis AnalyzePerformancePatterns(List<Nexo.Feature.AI.Interfaces.ProcessingMetrics> metrics)
        {
            return new PerformanceAnalysis
            {
                AverageProcessingTime = metrics.Average(m => m.ProcessingTime.TotalMilliseconds),
                ProcessingTimeVariance = CalculateVariance(metrics.Select(m => m.ProcessingTime.TotalMilliseconds)),
                SuccessRate = (double)metrics.Count(m => m.Success) / metrics.Count,
                ResourceUtilizationTrend = metrics.Average(m => m.ResourceUtilization)
            };
        }

        private List<string> IdentifyBottlenecks(List<Nexo.Feature.AI.Interfaces.ProcessingMetrics> metrics)
        {
            var bottlenecks = new List<string>();
            
            var avgProcessingTime = metrics.Average(m => m.ProcessingTime.TotalMilliseconds);
            var slowRequests = metrics.Where(m => m.ProcessingTime.TotalMilliseconds > avgProcessingTime * 2);
            
            if (slowRequests.Any())
                bottlenecks.Add($"Slow processing: {slowRequests.Count()} requests taking >{avgProcessingTime * 2}ms");
            
            var failureRate = 1.0 - (double)metrics.Count(m => m.Success) / metrics.Count;
            if (failureRate > 0.1)
                bottlenecks.Add($"High failure rate: {failureRate:P1}");
            
            return bottlenecks;
        }

        private List<string> GenerateOptimizationRecommendations(PerformanceAnalysis analysis, List<string> bottlenecks)
        {
            var recommendations = new List<string>();
            
            if (analysis.ProcessingTimeVariance > 1000) // High variance
                recommendations.Add("Consider request batching to reduce processing time variance");
            
            if (analysis.AverageProcessingTime > 5000) // Slow processing
                recommendations.Add("Consider increasing parallelism or optimizing request processing");
            
            if (analysis.SuccessRate < 0.9) // Low success rate
                recommendations.Add("Investigate and fix processing failures");
            
            recommendations.AddRange(bottlenecks.Select(b => $"Address bottleneck: {b}"));
            
            return recommendations;
        }

        private ProcessingStrategy UpdateStrategyBasedOnMetrics(PerformanceAnalysis analysis)
        {
            var strategy = GetDefaultStrategy();
            
            if (analysis.AverageProcessingTime > 5000)
                strategy.MaxParallelism = Math.Max(1, strategy.MaxParallelism - 1);
            
            if (analysis.ProcessingTimeVariance > 1000)
                strategy.BatchSize = Math.Max(1, strategy.BatchSize - 1);
            
            return strategy;
        }

        private double CalculateExpectedImprovement(PerformanceAnalysis analysis, List<string> recommendations)
        {
            // Simple improvement estimation
            var improvement = 0.0;
            
            if (analysis.AverageProcessingTime > 5000)
                improvement += 0.2; // 20% improvement expected
            
            if (analysis.SuccessRate < 0.9)
                improvement += 0.1; // 10% improvement expected
            
            return Math.Min(improvement, 0.5); // Cap at 50%
        }

        private List<Nexo.Feature.AI.Interfaces.ProcessingTrend> AnalyzeProcessingTrends(List<Nexo.Feature.AI.Interfaces.ProcessingMetrics> metrics)
        {
            // Group by time periods and analyze trends
            var trends = new List<Nexo.Feature.AI.Interfaces.ProcessingTrend>();
            
            var recentMetrics = metrics.Where(m => m.Timestamp > DateTime.UtcNow.AddHours(-1)).ToList();
            var olderMetrics = metrics.Where(m => m.Timestamp <= DateTime.UtcNow.AddHours(-1)).ToList();
            
            if (recentMetrics.Any() && olderMetrics.Any())
            {
                var recentAvg = recentMetrics.Average(m => m.ProcessingTime.TotalMilliseconds);
                var olderAvg = olderMetrics.Average(m => m.ProcessingTime.TotalMilliseconds);
                
                trends.Add(new Nexo.Feature.AI.Interfaces.ProcessingTrend
                {
                    Metric = "Processing Time",
                    Trend = recentAvg < olderAvg ? Nexo.Feature.AI.Interfaces.TrendDirection.Improving : Nexo.Feature.AI.Interfaces.TrendDirection.Degrading,
                    ChangePercentage = Math.Abs((recentAvg - olderAvg) / olderAvg * 100)
                });
            }
            
            return trends;
        }

        private double CalculateVariance(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (!list.Any()) return 0.0;
            
            var mean = list.Average();
            var variance = list.Select(x => Math.Pow(x - mean, 2)).Average();
            return variance;
        }

        private ProcessingStrategy GetDefaultStrategy()
        {
            return new ProcessingStrategy
            {
                MaxParallelism = Environment.ProcessorCount,
                BatchSize = 5,
                PriorityLevel = PriorityLevel.Normal,
                EstimatedDuration = TimeSpan.FromMinutes(5)
            };
        }
    }

    /// <summary>
    /// Analysis of request characteristics for optimization.
    /// </summary>
    public class RequestAnalysis
    {
        public int TotalRequests { get; set; }
        public double AverageInputLength { get; set; }
        public int MaxInputLength { get; set; }
        public int MinInputLength { get; set; }
        public Dictionary<string, int> RequestTypes { get; set; } = new Dictionary<string, int>();
        public double ComplexityScore { get; set; }
    }

    /// <summary>
    /// Analysis of processing performance patterns.
    /// </summary>
    public class PerformanceAnalysis
    {
        public double AverageProcessingTime { get; set; }
        public double ProcessingTimeVariance { get; set; }
        public double SuccessRate { get; set; }
        public double ResourceUtilizationTrend { get; set; }
    }

    /// <summary>
    /// Processing metrics for optimization.
    /// </summary>



} 