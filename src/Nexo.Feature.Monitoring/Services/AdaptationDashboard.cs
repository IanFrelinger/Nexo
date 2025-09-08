using System.Runtime.CompilerServices;

namespace Nexo.Feature.Monitoring.Services;

/// <summary>
/// Real-time adaptation dashboard service
/// </summary>
public class AdaptationDashboard : IAdaptationDashboard
{
    private readonly IAdaptationEngine _adaptationEngine;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IMetricsAggregator _metricsAggregator;
    private readonly IContinuousLearningSystem _learningSystem;
    private readonly IEnvironmentAdaptationService _environmentService;
    private readonly ILogger<AdaptationDashboard> _logger;
    
    public AdaptationDashboard(
        IAdaptationEngine adaptationEngine,
        IPerformanceMonitor performanceMonitor,
        IMetricsAggregator metricsAggregator,
        IContinuousLearningSystem learningSystem,
        IEnvironmentAdaptationService environmentService,
        ILogger<AdaptationDashboard> logger)
    {
        _adaptationEngine = adaptationEngine;
        _performanceMonitor = performanceMonitor;
        _metricsAggregator = metricsAggregator;
        _learningSystem = learningSystem;
        _environmentService = environmentService;
        _logger = logger;
    }
    
    public async Task<AdaptationDashboardData> GetRealTimeDashboardDataAsync()
    {
        _logger.LogDebug("Getting real-time dashboard data");
        
        var dashboardData = new AdaptationDashboardData();
        
        try
        {
            // Current adaptation status
            dashboardData.AdaptationStatus = await _adaptationEngine.GetAdaptationStatusAsync();
            
            // Real-time performance metrics
            dashboardData.PerformanceMetrics = await _performanceMonitor.GetCurrentMetricsAsync();
            
            // Recent adaptations
            dashboardData.RecentAdaptations = await GetRecentAdaptationsAsync(TimeSpan.FromHours(1));
            
            // Performance trends
            dashboardData.PerformanceTrends = await GetPerformanceTrendsAsync(TimeSpan.FromDays(1));
            
            // Learning insights
            dashboardData.LearningInsights = await GetCurrentLearningInsightsAsync();
            
            // Adaptation effectiveness
            dashboardData.AdaptationEffectiveness = await CalculateAdaptationEffectivenessAsync();
            
            // Environment status
            dashboardData.EnvironmentStatus = await GetEnvironmentAdaptationStatusAsync();
            
            dashboardData.LastUpdated = DateTime.UtcNow;
            
            _logger.LogDebug("Successfully retrieved dashboard data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
        }
        
        return dashboardData;
    }
    
    public async IAsyncEnumerable<AdaptationEvent> StreamAdaptationEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting adaptation event streaming");
        
        var lastEventTime = DateTime.UtcNow;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var newEvents = await GetAdaptationEventsSinceAsync(lastEventTime);
                
                foreach (var adaptationEvent in newEvents)
                {
                    yield return adaptationEvent;
                    lastEventTime = adaptationEvent.Timestamp;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in adaptation event streaming");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        
        _logger.LogInformation("Stopped adaptation event streaming");
    }
    
    public async Task<AdaptationEffectiveness> GetAdaptationEffectivenessAsync()
    {
        return await CalculateAdaptationEffectivenessAsync();
    }
    
    public async Task<IEnumerable<PerformanceTrend>> GetPerformanceTrendsAsync(TimeSpan timeWindow)
    {
        _logger.LogDebug("Getting performance trends for {TimeWindow}", timeWindow);
        
        var trends = new List<PerformanceTrend>();
        var endTime = DateTime.UtcNow;
        var startTime = endTime - timeWindow;
        
        // Get performance data points
        var performanceData = await _performanceMonitor.GetHistoricalDataAsync(timeWindow);
        
        // Group by time intervals (e.g., every 5 minutes)
        var interval = TimeSpan.FromMinutes(5);
        var currentTime = startTime;
        
        while (currentTime < endTime)
        {
            var intervalEnd = currentTime + interval;
            var intervalData = performanceData
                .Where(p => p.Timestamp >= currentTime && p.Timestamp < intervalEnd)
                .ToList();
            
            if (intervalData.Any())
            {
                trends.Add(new PerformanceTrend
                {
                    Timestamp = currentTime,
                    CpuUtilization = intervalData.Average(p => p.CpuUtilization),
                    MemoryUtilization = intervalData.Average(p => p.MemoryUtilization),
                    ResponseTime = intervalData.Average(p => p.ResponseTime),
                    Throughput = intervalData.Average(p => p.Throughput),
                    OverallScore = intervalData.Average(p => p.OverallScore)
                });
            }
            
            currentTime = intervalEnd;
        }
        
        return trends;
    }
    
    public async Task<LearningDashboard> GetLearningDashboardAsync()
    {
        _logger.LogDebug("Getting learning dashboard data");
        
        var dashboard = new LearningDashboard();
        
        try
        {
            // Learning effectiveness
            dashboard.LearningEffectiveness = await _learningSystem.GetLearningEffectivenessAsync();
            
            // Recent insights
            dashboard.RecentInsights = await _learningSystem.GetCurrentInsightsAsync();
            
            // Active recommendations
            dashboard.ActiveRecommendations = await GetActiveRecommendationsAsync();
            
            // Learning metrics
            dashboard.LearningMetrics = await CalculateLearningMetricsAsync();
            
            // Last learning cycle
            dashboard.LastLearningCycle = dashboard.RecentInsights.Any() 
                ? dashboard.RecentInsights.Max(i => i.DiscoveredAt)
                : DateTime.MinValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning dashboard data");
        }
        
        return dashboard;
    }
    
    public async Task<EnvironmentAdaptationStatus> GetEnvironmentAdaptationStatusAsync()
    {
        _logger.LogDebug("Getting environment adaptation status");
        
        var status = new EnvironmentAdaptationStatus();
        
        try
        {
            // Current environment
            var environmentDetector = _environmentService.GetType()
                .GetProperty("_environmentDetector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_environmentService) as IEnvironmentDetector;
            
            if (environmentDetector != null)
            {
                status.CurrentEnvironment = await environmentDetector.DetectCurrentEnvironmentAsync();
            }
            
            // Active optimizations
            status.ActiveOptimizations = await _environmentService.GetEnvironmentOptimizationsAsync(status.CurrentEnvironment);
            
            // Recent changes
            status.RecentChanges = await GetRecentEnvironmentChangesAsync(TimeSpan.FromHours(24));
            
            // Validation result
            status.ValidationResult = await _environmentService.ValidateEnvironmentAsync(status.CurrentEnvironment);
            
            status.LastEnvironmentCheck = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment adaptation status");
        }
        
        return status;
    }
    
    private async Task<IEnumerable<AppliedAdaptation>> GetRecentAdaptationsAsync(TimeSpan timeWindow)
    {
        var adaptations = await _adaptationEngine.GetRecentAdaptationsAsync(timeWindow);
        return adaptations.Select(r => new AppliedAdaptation
        {
            Id = r.Id,
            Type = r.Type.ToString(),
            Description = $"Adaptation applied: {r.Type}",
            AppliedAt = r.AppliedAt,
            EstimatedImprovementFactor = r.EffectivenessScore,
            StrategyId = r.StrategyId
        });
    }
    
    private async Task<IEnumerable<LearningInsight>> GetCurrentLearningInsightsAsync()
    {
        return await _learningSystem.GetCurrentInsightsAsync();
    }
    
    private async Task<AdaptationEffectiveness> CalculateAdaptationEffectivenessAsync()
    {
        var effectiveness = new AdaptationEffectiveness();
        
        try
        {
            var recentAdaptations = await _adaptationEngine.GetRecentAdaptationsAsync(TimeSpan.FromHours(24));
            var performanceData = await _performanceMonitor.GetHistoricalDataAsync(TimeSpan.FromHours(24));
            
            var adaptationResults = new List<AdaptationResult>();
            
            foreach (var adaptation in recentAdaptations)
            {
                var beforePerformance = performanceData
                    .Where(p => p.Timestamp <= adaptation.AppliedAt)
                    .TakeLast(10)
                    .ToList();
                
                var afterPerformance = performanceData
                    .Where(p => p.Timestamp > adaptation.AppliedAt)
                    .Take(10)
                    .ToList();
                
                if (beforePerformance.Any() && afterPerformance.Any())
                {
                    var beforeScore = beforePerformance.Average(p => p.OverallScore);
                    var afterScore = afterPerformance.Average(p => p.OverallScore);
                    var improvement = (afterScore - beforeScore) / beforeScore;
                    
                    adaptationResults.Add(new AdaptationResult
                    {
                        AdaptationId = adaptation.Id,
                        AdaptationType = adaptation.Type,
                        ExpectedImprovement = 0.1, // Would be from actual adaptation data
                        ActualImprovement = improvement,
                        EffectivenessScore = improvement / 0.1, // Normalize against expected
                        AppliedAt = adaptation.AppliedAt,
                        StrategyId = adaptation.StrategyId
                    });
                }
            }
            
            effectiveness.AdaptationResults = adaptationResults;
            effectiveness.OverallEffectiveness = adaptationResults.Any() 
                ? adaptationResults.Average(r => r.EffectivenessScore)
                : 0;
            
            effectiveness.TotalAdaptations = recentAdaptations.Count();
            effectiveness.SuccessfulAdaptations = adaptationResults.Count(r => r.EffectivenessScore > 0);
            effectiveness.AverageImprovement = adaptationResults.Any() 
                ? adaptationResults.Average(r => r.ActualImprovement)
                : 0;
            
            // Calculate effectiveness by type
            var resultsByType = adaptationResults.GroupBy(r => r.AdaptationType);
            foreach (var group in resultsByType)
            {
                effectiveness.EffectivenessByType[group.Key.ToString()] = group.Average(r => r.EffectivenessScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating adaptation effectiveness");
        }
        
        return effectiveness;
    }
    
    private async Task<IEnumerable<AdaptationEvent>> GetAdaptationEventsSinceAsync(DateTime since)
    {
        var events = new List<AdaptationEvent>();
        
        try
        {
            // Get recent adaptations
            var recentAdaptations = await _adaptationEngine.GetRecentAdaptationsAsync(DateTime.UtcNow - since);
            
            foreach (var adaptation in recentAdaptations.Where(a => a.AppliedAt > since))
            {
                events.Add(new AdaptationEvent
                {
                    Type = AdaptationEventType.AdaptationApplied,
                    Description = $"Adaptation applied: {adaptation.Type}",
                    Timestamp = adaptation.AppliedAt,
                    EventData = new Dictionary<string, object>
                    {
                        ["AdaptationId"] = adaptation.Id,
                        ["AdaptationType"] = adaptation.Type.ToString(),
                        ["StrategyId"] = adaptation.StrategyId
                    }
                });
            }
            
            // Get performance events
            var performanceMetrics = await _performanceMonitor.GetCurrentMetricsAsync();
            if (performanceMetrics.Severity >= PerformanceSeverity.High)
            {
                events.Add(new AdaptationEvent
                {
                    Type = AdaptationEventType.PerformanceDegradation,
                    Description = $"Performance degradation detected: {performanceMetrics.Severity}",
                    Timestamp = DateTime.UtcNow,
                    PerformanceImpact = -0.1, // Negative impact
                    EventData = new Dictionary<string, object>
                    {
                        ["Severity"] = performanceMetrics.Severity.ToString(),
                        ["CpuUtilization"] = performanceMetrics.CpuUtilization,
                        ["MemoryUtilization"] = performanceMetrics.MemoryUtilization
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting adaptation events");
        }
        
        return events;
    }
    
    private async Task<IEnumerable<LearningRecommendation>> GetActiveRecommendationsAsync()
    {
        // This would integrate with the actual recommendation system
        return Enumerable.Empty<LearningRecommendation>();
    }
    
    private async Task<Dictionary<string, double>> CalculateLearningMetricsAsync()
    {
        var metrics = new Dictionary<string, double>();
        
        try
        {
            var effectiveness = await _learningSystem.GetLearningEffectivenessAsync();
            
            metrics["OverallEffectiveness"] = effectiveness.OverallEffectiveness;
            metrics["SuccessRate"] = effectiveness.SuccessRate;
            metrics["AverageImprovement"] = effectiveness.AverageImprovement;
            metrics["TotalInsights"] = effectiveness.TotalInsightsGenerated;
            metrics["AppliedInsights"] = effectiveness.AppliedInsights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating learning metrics");
        }
        
        return metrics;
    }
    
    private async Task<IEnumerable<EnvironmentChange>> GetRecentEnvironmentChangesAsync(TimeSpan timeWindow)
    {
        // This would integrate with the actual environment change tracking
        return Enumerable.Empty<EnvironmentChange>();
    }
}

/// <summary>
/// Interface for metrics aggregation
/// </summary>
public interface IMetricsAggregator
{
    Task<Dictionary<string, double>> AggregateMetricsAsync(TimeSpan timeWindow);
    Task<double> CalculateTrendAsync(string metricName, TimeSpan timeWindow);
    Task<IEnumerable<MetricDataPoint>> GetMetricHistoryAsync(string metricName, TimeSpan timeWindow);
}

/// <summary>
/// Metric data point
/// </summary>
public class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, object> Tags { get; set; } = new();
}