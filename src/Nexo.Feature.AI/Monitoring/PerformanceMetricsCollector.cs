using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.AI.Monitoring;

/// <summary>
/// Collects and aggregates performance metrics for all agents
/// </summary>
public class PerformanceMetricsCollector : IPerformanceMetricsCollector
{
    private readonly ILogger<PerformanceMetricsCollector> _logger;
    private readonly Dictionary<string, List<AgentPerformanceMetrics>> _agentMetrics = new();
    private readonly Dictionary<string, AgentPerformanceMetrics> _currentMetrics = new();
    
    public PerformanceMetricsCollector(ILogger<PerformanceMetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<IEnumerable<AgentPerformanceMetrics>> GetCurrentMetricsAsync()
    {
        try
        {
            _logger.LogDebug("Getting current metrics for all agents");
            
            // Return current aggregated metrics for all agents
            var currentMetrics = _currentMetrics.Values.ToList();
            
            _logger.LogDebug("Retrieved metrics for {AgentCount} agents", currentMetrics.Count);
            return currentMetrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current metrics");
            return Enumerable.Empty<AgentPerformanceMetrics>();
        }
    }
    
    public async Task<AgentPerformanceMetrics> GetAgentMetricsAsync(string agentId)
    {
        try
        {
            _logger.LogDebug("Getting metrics for agent {AgentId}", agentId);
            
            if (_currentMetrics.TryGetValue(agentId, out var metrics))
            {
                return metrics;
            }
            
            // Return default metrics if no data available
            return new AgentPerformanceMetrics
            {
                AgentId = agentId,
                SuccessRate = 0.0,
                AverageResponseTime = TimeSpan.Zero,
                AverageConfidence = 0.0,
                TotalRequests = 0,
                SuccessfulRequests = 0,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for agent {AgentId}", agentId);
            return new AgentPerformanceMetrics
            {
                AgentId = agentId,
                SuccessRate = 0.0,
                AverageResponseTime = TimeSpan.Zero,
                AverageConfidence = 0.0,
                TotalRequests = 0,
                SuccessfulRequests = 0,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
    
    public async Task RecordAgentMetricsAsync(string agentId, AgentPerformanceMetrics metrics)
    {
        try
        {
            _logger.LogDebug("Recording metrics for agent {AgentId}", agentId);
            
            // Store individual metrics record
            if (!_agentMetrics.ContainsKey(agentId))
            {
                _agentMetrics[agentId] = new List<AgentPerformanceMetrics>();
            }
            
            _agentMetrics[agentId].Add(metrics);
            
            // Keep only the last 1000 records per agent
            if (_agentMetrics[agentId].Count > 1000)
            {
                _agentMetrics[agentId] = _agentMetrics[agentId]
                    .OrderByDescending(m => m.LastUpdated)
                    .Take(1000)
                    .ToList();
            }
            
            // Update current aggregated metrics
            await UpdateCurrentMetrics(agentId);
            
            _logger.LogDebug("Recorded metrics for agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metrics for agent {AgentId}", agentId);
        }
    }
    
    private async Task UpdateCurrentMetrics(string agentId)
    {
        try
        {
            if (!_agentMetrics.ContainsKey(agentId) || !_agentMetrics[agentId].Any())
            {
                return;
            }
            
            var recentMetrics = _agentMetrics[agentId]
                .Where(m => m.LastUpdated >= DateTime.UtcNow.AddHours(-1)) // Last hour
                .ToList();
            
            if (!recentMetrics.Any())
            {
                recentMetrics = _agentMetrics[agentId].Take(100).ToList();
            }
            
            var aggregatedMetrics = new AgentPerformanceMetrics
            {
                AgentId = agentId,
                SuccessRate = recentMetrics.Count(m => m.SuccessRate > 0.8) / (double)recentMetrics.Count,
                AverageResponseTime = TimeSpan.FromMilliseconds(recentMetrics.Average(m => m.AverageResponseTime.TotalMilliseconds)),
                AverageConfidence = recentMetrics.Average(m => m.AverageConfidence),
                TotalRequests = recentMetrics.Sum(m => m.TotalRequests),
                SuccessfulRequests = recentMetrics.Sum(m => m.SuccessfulRequests),
                LastUpdated = DateTime.UtcNow
            };
            
            _currentMetrics[agentId] = aggregatedMetrics;
            
            _logger.LogDebug("Updated current metrics for agent {AgentId}: SuccessRate={SuccessRate}, AvgResponseTime={AvgResponseTime}ms", 
                agentId, aggregatedMetrics.SuccessRate, aggregatedMetrics.AverageResponseTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating current metrics for agent {AgentId}", agentId);
        }
    }
    
    public void RecordAgentRequest(string agentId, bool success, TimeSpan responseTime, double confidence)
    {
        try
        {
            // Create a new metrics record for this request
            var metrics = new AgentPerformanceMetrics
            {
                AgentId = agentId,
                SuccessRate = success ? 1.0 : 0.0,
                AverageResponseTime = responseTime,
                AverageConfidence = confidence,
                TotalRequests = 1,
                SuccessfulRequests = success ? 1 : 0,
                LastUpdated = DateTime.UtcNow
            };
            
            // Record the metrics asynchronously
            _ = Task.Run(async () => await RecordAgentMetricsAsync(agentId, metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording agent request for {AgentId}", agentId);
        }
    }
    
    public async Task<SystemPerformanceSummary> GetSystemPerformanceSummaryAsync()
    {
        try
        {
            var allMetrics = await GetCurrentMetricsAsync();
            
            if (!allMetrics.Any())
            {
                return new SystemPerformanceSummary
                {
                    TotalAgents = 0,
                    OverallSuccessRate = 0.0,
                    AverageResponseTime = TimeSpan.Zero,
                    HighPerformingAgents = 0,
                    UnderperformingAgents = 0
                };
            }
            
            var summary = new SystemPerformanceSummary
            {
                TotalAgents = allMetrics.Count(),
                OverallSuccessRate = allMetrics.Average(m => m.SuccessRate),
                AverageResponseTime = TimeSpan.FromMilliseconds(allMetrics.Average(m => m.AverageResponseTime.TotalMilliseconds)),
                HighPerformingAgents = allMetrics.Count(m => m.SuccessRate > 0.9),
                UnderperformingAgents = allMetrics.Count(m => m.SuccessRate < 0.8),
                TotalRequests = allMetrics.Sum(m => m.TotalRequests),
                SuccessfulRequests = allMetrics.Sum(m => m.SuccessfulRequests)
            };
            
            _logger.LogDebug("Generated system performance summary: {TotalAgents} agents, {SuccessRate} success rate", 
                summary.TotalAgents, summary.OverallSuccessRate);
            
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system performance summary");
            return new SystemPerformanceSummary
            {
                TotalAgents = 0,
                OverallSuccessRate = 0.0,
                AverageResponseTime = TimeSpan.Zero,
                HighPerformingAgents = 0,
                UnderperformingAgents = 0
            };
        }
    }
}

/// <summary>
/// System performance summary
/// </summary>
public record SystemPerformanceSummary
{
    public int TotalAgents { get; init; }
    public double OverallSuccessRate { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
    public int HighPerformingAgents { get; init; }
    public int UnderperformingAgents { get; init; }
    public int TotalRequests { get; init; }
    public int SuccessfulRequests { get; init; }
}
