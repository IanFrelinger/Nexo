using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.AI.Learning;

/// <summary>
/// Stores and retrieves agent knowledge and performance data
/// </summary>
public class AgentKnowledgeStore : IAgentKnowledgeStore
{
    private readonly ILogger<AgentKnowledgeStore> _logger;
    private readonly Dictionary<string, List<AgentLearningRecord>> _performanceRecords = new();
    private readonly Dictionary<string, List<LearningInsight>> _learningInsights = new();
    
    public AgentKnowledgeStore(ILogger<AgentKnowledgeStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task StorePerformanceRecordAsync(AgentLearningRecord record)
    {
        try
        {
            _logger.LogDebug("Storing performance record for agent {AgentId}", record.AgentId);
            
            if (!_performanceRecords.ContainsKey(record.AgentId))
            {
                _performanceRecords[record.AgentId] = new List<AgentLearningRecord>();
            }
            
            _performanceRecords[record.AgentId].Add(record);
            
            // Keep only the last 1000 records per agent to prevent memory issues
            if (_performanceRecords[record.AgentId].Count > 1000)
            {
                _performanceRecords[record.AgentId] = _performanceRecords[record.AgentId]
                    .OrderByDescending(r => r.Timestamp)
                    .Take(1000)
                    .ToList();
            }
            
            _logger.LogDebug("Stored performance record for agent {AgentId}", record.AgentId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing performance record for agent {AgentId}", record.AgentId);
        }
    }
    
    public async Task<IEnumerable<AgentLearningRecord>> GetRecentRecordsAsync(string agentId, TimeSpan timeWindow)
    {
        try
        {
            _logger.LogDebug("Getting recent records for agent {AgentId} within {TimeWindow}", agentId, timeWindow);
            
            if (!_performanceRecords.ContainsKey(agentId))
            {
                return Enumerable.Empty<AgentLearningRecord>();
            }
            
            var cutoffTime = DateTime.UtcNow - timeWindow;
            var recentRecords = _performanceRecords[agentId]
                .Where(r => r.Timestamp >= cutoffTime)
                .OrderByDescending(r => r.Timestamp);
            
            _logger.LogDebug("Found {RecordCount} recent records for agent {AgentId}", recentRecords.Count(), agentId);
            return recentRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent records for agent {AgentId}", agentId);
            return Enumerable.Empty<AgentLearningRecord>();
        }
    }
    
    public async Task<AgentPerformanceAnalytics> GetPerformanceAnalyticsAsync(string agentId)
    {
        try
        {
            _logger.LogDebug("Getting performance analytics for agent {AgentId}", agentId);
            
            if (!_performanceRecords.ContainsKey(agentId) || !_performanceRecords[agentId].Any())
            {
                return new AgentPerformanceAnalytics
                {
                    OverallSuccessRate = 0.0,
                    AverageConfidence = 0.0,
                    AverageResponseTime = TimeSpan.Zero,
                    BestContexts = Array.Empty<string>(),
                    WeakestAreas = Array.Empty<string>()
                };
            }
            
            var records = _performanceRecords[agentId];
            var recentRecords = records.Where(r => r.Timestamp >= DateTime.UtcNow.AddDays(-30)).ToList();
            
            if (!recentRecords.Any())
            {
                recentRecords = records.Take(100).ToList();
            }
            
            var analytics = new AgentPerformanceAnalytics
            {
                OverallSuccessRate = recentRecords.Count(r => r.Success) / (double)recentRecords.Count,
                AverageConfidence = recentRecords.Average(r => r.ResponseConfidence),
                AverageResponseTime = TimeSpan.FromMilliseconds(recentRecords.Average(r => r.ActualPerformance.ExecutionTime.TotalMilliseconds)),
                BestContexts = GetBestContexts(recentRecords),
                WeakestAreas = GetWeakestAreas(recentRecords),
                ContextPerformance = GetContextPerformance(recentRecords)
            };
            
            _logger.LogDebug("Generated analytics for agent {AgentId}: SuccessRate={SuccessRate}, AvgConfidence={AvgConfidence}", 
                agentId, analytics.OverallSuccessRate, analytics.AverageConfidence);
            
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance analytics for agent {AgentId}", agentId);
            return new AgentPerformanceAnalytics
            {
                OverallSuccessRate = 0.0,
                AverageConfidence = 0.0,
                AverageResponseTime = TimeSpan.Zero,
                BestContexts = Array.Empty<string>(),
                WeakestAreas = Array.Empty<string>()
            };
        }
    }
    
    public async Task StoreLearningInsightAsync(string agentId, LearningInsight insight)
    {
        try
        {
            _logger.LogDebug("Storing learning insight for agent {AgentId}", agentId);
            
            if (!_learningInsights.ContainsKey(agentId))
            {
                _learningInsights[agentId] = new List<LearningInsight>();
            }
            
            _learningInsights[agentId].Add(insight);
            
            // Keep only the last 500 insights per agent
            if (_learningInsights[agentId].Count > 500)
            {
                _learningInsights[agentId] = _learningInsights[agentId]
                    .OrderByDescending(i => i.Timestamp)
                    .Take(500)
                    .ToList();
            }
            
            _logger.LogDebug("Stored learning insight for agent {AgentId}", agentId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing learning insight for agent {AgentId}", agentId);
        }
    }
    
    public async Task<IEnumerable<LearningInsight>> GetLearningInsightsAsync(string agentId)
    {
        try
        {
            _logger.LogDebug("Getting learning insights for agent {AgentId}", agentId);
            
            if (!_learningInsights.ContainsKey(agentId))
            {
                return Enumerable.Empty<LearningInsight>();
            }
            
            var insights = _learningInsights[agentId]
                .OrderByDescending(i => i.Timestamp)
                .Take(100); // Return the most recent 100 insights
            
            _logger.LogDebug("Found {InsightCount} learning insights for agent {AgentId}", insights.Count(), agentId);
            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning insights for agent {AgentId}", agentId);
            return Enumerable.Empty<LearningInsight>();
        }
    }
    
    private string[] GetBestContexts(List<AgentLearningRecord> records)
    {
        var contextPerformance = new Dictionary<string, (int SuccessCount, int TotalCount)>();
        
        foreach (var record in records)
        {
            if (record.RequestContext?.TryGetValue("Platform", out var platform) == true)
            {
                var platformStr = platform.ToString() ?? "Unknown";
                if (!contextPerformance.ContainsKey(platformStr))
                {
                    contextPerformance[platformStr] = (0, 0);
                }
                
                var current = contextPerformance[platformStr];
                contextPerformance[platformStr] = (
                    current.SuccessCount + (record.Success ? 1 : 0),
                    current.TotalCount + 1
                );
            }
        }
        
        return contextPerformance
            .Where(kvp => kvp.Value.TotalCount >= 5) // Only consider contexts with at least 5 samples
            .OrderByDescending(kvp => kvp.Value.SuccessCount / (double)kvp.Value.TotalCount)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToArray();
    }
    
    private string[] GetWeakestAreas(List<AgentLearningRecord> records)
    {
        var failurePatterns = new Dictionary<string, int>();
        
        foreach (var record in records.Where(r => !r.Success))
        {
            if (!string.IsNullOrEmpty(record.ActualPerformance.ErrorMessage))
            {
                var errorType = ClassifyError(record.ActualPerformance.ErrorMessage);
                failurePatterns[errorType] = failurePatterns.GetValueOrDefault(errorType, 0) + 1;
            }
        }
        
        return failurePatterns
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToArray();
    }
    
    private Dictionary<string, double> GetContextPerformance(List<AgentLearningRecord> records)
    {
        var contextPerformance = new Dictionary<string, double>();
        
        var contextGroups = records
            .Where(r => r.RequestContext?.ContainsKey("Platform") == true)
            .GroupBy(r => r.RequestContext!["Platform"].ToString() ?? "Unknown");
        
        foreach (var group in contextGroups)
        {
            var successRate = group.Count(r => r.Success) / (double)group.Count();
            contextPerformance[group.Key] = successRate;
        }
        
        return contextPerformance;
    }
    
    private string ClassifyError(string errorMessage)
    {
        var lowerError = errorMessage.ToLowerInvariant();
        
        if (lowerError.Contains("timeout"))
            return "Timeout";
        
        if (lowerError.Contains("memory") || lowerError.Contains("out of memory"))
            return "Memory";
        
        if (lowerError.Contains("network") || lowerError.Contains("connection"))
            return "Network";
        
        if (lowerError.Contains("validation") || lowerError.Contains("invalid"))
            return "Validation";
        
        if (lowerError.Contains("permission") || lowerError.Contains("access"))
            return "Permission";
        
        return "General";
    }
}
