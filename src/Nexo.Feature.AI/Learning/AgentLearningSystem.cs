using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Learning;

/// <summary>
/// System for learning from agent performance and improving future results
/// </summary>
public class AgentLearningSystem : IAgentLearningSystem
{
    private readonly IPerformanceFeedbackCollector _feedbackCollector;
    private readonly IAgentKnowledgeStore _knowledgeStore;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<AgentLearningSystem> _logger;
    
    public AgentLearningSystem(
        IPerformanceFeedbackCollector feedbackCollector,
        IAgentKnowledgeStore knowledgeStore,
        IModelOrchestrator modelOrchestrator,
        ILogger<AgentLearningSystem> logger)
    {
        _feedbackCollector = feedbackCollector ?? throw new ArgumentNullException(nameof(feedbackCollector));
        _knowledgeStore = knowledgeStore ?? throw new ArgumentNullException(nameof(knowledgeStore));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task RecordAgentPerformance(
        string agentId, 
        AgentRequest request, 
        AgentResponse response, 
        PerformanceMetrics actualPerformance)
    {
        try
        {
            _logger.LogDebug("Recording performance for agent {AgentId}", agentId);
            
            var learningRecord = new AgentLearningRecord
            {
                AgentId = agentId,
                RequestContext = new Dictionary<string, object>
                {
                    ["Context"] = request.Context,
                    ["Parameters"] = request.Parameters
                },
                GeneratedCode = response.Result,
                ActualPerformance = actualPerformance,
                ResponseConfidence = response.Confidence,
                Success = response.Success,
                Timestamp = DateTime.UtcNow
            };
            
            await _knowledgeStore.StorePerformanceRecordAsync(learningRecord);
            
            // Trigger learning if we have enough data
            var recentRecords = await _knowledgeStore.GetRecentRecordsAsync(agentId, TimeSpan.FromDays(7));
            if (recentRecords.Count() >= 10)
            {
                await TriggerLearningCycle(agentId, recentRecords);
            }
            
            _logger.LogDebug("Performance recorded for agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording agent performance for {AgentId}", agentId);
        }
    }
    
    public async Task<AgentCapabilityImprovements> GetRecommendedImprovements(string agentId)
    {
        try
        {
            _logger.LogDebug("Getting recommended improvements for agent {AgentId}", agentId);
            
            var performanceData = await _knowledgeStore.GetPerformanceAnalyticsAsync(agentId);
            
            var improvements = new AgentCapabilityImprovements
            {
                SuccessRate = performanceData.OverallSuccessRate,
                TopPerformingContexts = performanceData.BestContexts,
                AreasForImprovement = performanceData.WeakestAreas,
                RecommendedTraining = GenerateTrainingRecommendations(performanceData),
                ConfidenceTrend = CalculateConfidenceTrend(performanceData),
                PerformanceTrend = CalculatePerformanceTrend(performanceData)
            };
            
            _logger.LogDebug("Generated {ImprovementCount} recommendations for agent {AgentId}", 
                improvements.RecommendedTraining.Count(), agentId);
            
            return improvements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended improvements for agent {AgentId}", agentId);
            return new AgentCapabilityImprovements
            {
                SuccessRate = 0.0,
                TopPerformingContexts = Array.Empty<string>(),
                AreasForImprovement = Array.Empty<string>(),
                RecommendedTraining = Array.Empty<string>()
            };
        }
    }
    
    public async Task ApplyLearningInsights(string agentId, IEnumerable<LearningInsight> insights)
    {
        try
        {
            _logger.LogInformation("Applying {InsightCount} learning insights to agent {AgentId}", 
                insights.Count(), agentId);
            
            foreach (var insight in insights)
            {
                await _knowledgeStore.StoreLearningInsightAsync(agentId, insight);
            }
            
            // Update agent's knowledge base with new insights
            await UpdateAgentKnowledgeBase(agentId, insights);
            
            _logger.LogInformation("Successfully applied learning insights to agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying learning insights to agent {AgentId}", agentId);
        }
    }
    
    private async Task TriggerLearningCycle(string agentId, IEnumerable<AgentLearningRecord> records)
    {
        try
        {
            _logger.LogInformation("Triggering learning cycle for agent {AgentId} with {RecordCount} records", 
                agentId, records.Count());
            
            // Analyze patterns in successful vs unsuccessful optimizations
            var learningPrompt = $"""
            Analyze these agent performance records to identify learning patterns:
            
            Agent: {agentId}
            Records: {records.Count()} samples from the last 7 days
            
            {FormatLearningRecords(records)}
            
            Identify:
            1. What optimization strategies work best for different contexts?
            2. Which platforms show the best improvement rates?
            3. What request patterns predict successful optimizations?
            4. Are there failure patterns to avoid?
            5. How can the agent improve its capability assessment?
            6. What are the most effective prompt patterns?
            7. Which optimization types yield the best results?
            8. How does performance vary by request complexity?
            
            Generate improved decision-making strategies and prompt optimizations for this agent.
            """;
            
            var modelRequest = new Models.ModelRequest
            {
                Input = learningPrompt,
                Temperature = 0.3,
                MaxTokens = 2000
            };
            
            var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
            
            if (response.Success)
            {
                // Parse learning insights from the response
                var insights = ParseLearningInsights(response.Response);
                
                // Store insights for the agent
                await ApplyLearningInsights(agentId, insights);
                
                _logger.LogInformation("Learning cycle completed for agent {AgentId} with {InsightCount} insights", 
                    agentId, insights.Count());
            }
            else
            {
                _logger.LogWarning("Failed to generate learning insights for agent {AgentId}", agentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during learning cycle for agent {AgentId}", agentId);
        }
    }
    
    private string FormatLearningRecords(IEnumerable<AgentLearningRecord> records)
    {
        var formatted = new List<string>();
        
        foreach (var record in records.Take(20)) // Limit to avoid token limits
        {
            formatted.Add($"""
                Request: {record.RequestContext?.GetValueOrDefault("Input", "N/A")}
                Success: {record.Success}
                Confidence: {record.ResponseConfidence}
                Performance: {record.ActualPerformance.ExecutionTime.TotalMilliseconds}ms
                """);
        }
        
        return string.Join("\n", formatted);
    }
    
    private IEnumerable<LearningInsight> ParseLearningInsights(string response)
    {
        var insights = new List<LearningInsight>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("-") || line.Trim().StartsWith("•"))
            {
                var insightText = line.Trim().TrimStart('-', '•').Trim();
                
                insights.Add(new LearningInsight
                {
                    Type = DetermineInsightType(insightText),
                    Description = insightText,
                    Confidence = 0.8,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        
        return insights;
    }
    
    private LearningInsightType DetermineInsightType(string insightText)
    {
        var lowerText = insightText.ToLowerInvariant();
        
        if (lowerText.Contains("prompt") || lowerText.Contains("instruction"))
            return LearningInsightType.PromptOptimization;
        
        if (lowerText.Contains("strategy") || lowerText.Contains("approach"))
            return LearningInsightType.StrategyImprovement;
        
        if (lowerText.Contains("context") || lowerText.Contains("pattern"))
            return LearningInsightType.ContextAnalysis;
        
        if (lowerText.Contains("performance") || lowerText.Contains("optimization"))
            return LearningInsightType.PerformanceOptimization;
        
        return LearningInsightType.General;
    }
    
    private IEnumerable<string> GenerateTrainingRecommendations(AgentPerformanceAnalytics analytics)
    {
        var recommendations = new List<string>();
        
        if (analytics.OverallSuccessRate < 0.8)
        {
            recommendations.Add("Focus on improving success rate through better error handling");
        }
        
        if (analytics.AverageConfidence < 0.7)
        {
            recommendations.Add("Improve confidence assessment through better context analysis");
        }
        
        if (analytics.WeakestAreas.Any())
        {
            recommendations.Add($"Address weaknesses in: {string.Join(", ", analytics.WeakestAreas)}");
        }
        
        if (analytics.BestContexts.Any())
        {
            recommendations.Add($"Leverage strengths in: {string.Join(", ", analytics.BestContexts)}");
        }
        
        if (analytics.AverageResponseTime > TimeSpan.FromSeconds(10))
        {
            recommendations.Add("Optimize response time through prompt efficiency");
        }
        
        return recommendations;
    }
    
    private double CalculateConfidenceTrend(AgentPerformanceAnalytics analytics)
    {
        // Simple trend calculation - in reality, this would be more sophisticated
        return analytics.AverageConfidence > 0.7 ? 1.0 : 0.5;
    }
    
    private double CalculatePerformanceTrend(AgentPerformanceAnalytics analytics)
    {
        // Simple trend calculation - in reality, this would be more sophisticated
        return analytics.OverallSuccessRate > 0.8 ? 1.0 : 0.5;
    }
    
    private async Task UpdateAgentKnowledgeBase(string agentId, IEnumerable<LearningInsight> insights)
    {
        try
        {
            // Store insights in the knowledge base for future reference
            foreach (var insight in insights)
            {
                await _knowledgeStore.StoreLearningInsightAsync(agentId, insight);
            }
            
            _logger.LogDebug("Updated knowledge base for agent {AgentId} with {InsightCount} insights", 
                agentId, insights.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating knowledge base for agent {AgentId}", agentId);
        }
    }
}

/// <summary>
/// Interface for agent learning system
/// </summary>
public interface IAgentLearningSystem
{
    Task RecordAgentPerformance(string agentId, AgentRequest request, AgentResponse response, PerformanceMetrics actualPerformance);
    Task<AgentCapabilityImprovements> GetRecommendedImprovements(string agentId);
    Task ApplyLearningInsights(string agentId, IEnumerable<LearningInsight> insights);
}

/// <summary>
/// Agent learning record
/// </summary>
public record AgentLearningRecord
{
    public string AgentId { get; init; } = string.Empty;
    public Dictionary<string, object>? RequestContext { get; init; }
    public string GeneratedCode { get; init; } = string.Empty;
    public PerformanceMetrics ActualPerformance { get; init; } = new();
    public double ResponseConfidence { get; init; }
    public bool Success { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Learning insight from performance analysis
/// </summary>
public record LearningInsight
{
    public LearningInsightType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Types of learning insights
/// </summary>
public enum LearningInsightType
{
    General,
    PromptOptimization,
    StrategyImprovement,
    ContextAnalysis,
    PerformanceOptimization,
    ErrorPattern
}

/// <summary>
/// Agent capability improvements
/// </summary>
public record AgentCapabilityImprovements
{
    public double SuccessRate { get; init; }
    public string[] TopPerformingContexts { get; init; } = [];
    public string[] AreasForImprovement { get; init; } = [];
    public IEnumerable<string> RecommendedTraining { get; init; } = [];
    public double ConfidenceTrend { get; init; }
    public double PerformanceTrend { get; init; }
}

/// <summary>
/// Agent performance analytics
/// </summary>
public record AgentPerformanceAnalytics
{
    public double OverallSuccessRate { get; init; }
    public double AverageConfidence { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
    public string[] BestContexts { get; init; } = [];
    public string[] WeakestAreas { get; init; } = [];
    public Dictionary<string, double> ContextPerformance { get; init; } = new();
}

/// <summary>
/// Interface for performance feedback collection
/// </summary>
public interface IPerformanceFeedbackCollector
{
    Task<PerformanceMetrics> CollectMetricsAsync(string agentId, AgentRequest request, AgentResponse response);
    Task RecordFeedbackAsync(string agentId, AgentRequest request, AgentResponse response, PerformanceMetrics metrics);
}

/// <summary>
/// Interface for agent knowledge storage
/// </summary>
public interface IAgentKnowledgeStore
{
    Task StorePerformanceRecordAsync(AgentLearningRecord record);
    Task<IEnumerable<AgentLearningRecord>> GetRecentRecordsAsync(string agentId, TimeSpan timeWindow);
    Task<AgentPerformanceAnalytics> GetPerformanceAnalyticsAsync(string agentId);
    Task StoreLearningInsightAsync(string agentId, LearningInsight insight);
    Task<IEnumerable<LearningInsight>> GetLearningInsightsAsync(string agentId);
}
