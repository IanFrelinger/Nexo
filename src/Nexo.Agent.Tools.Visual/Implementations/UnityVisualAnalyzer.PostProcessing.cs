using Nexo.Agent.Tools.Visual.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Post-processing functionality for Unity visual analyzer.
/// </summary>
public sealed partial class UnityVisualAnalyzer
{
    private VisualAnalysisResult PostProcessForUnity(VisualAnalysisResult result, string analysisType)
    {
        if (!result.Success)
            return result;

        // Add Unity-specific insights
        var unityInsights = new List<VisualInsight>();
        
        // Add Unity-specific metrics
        var unityMetrics = new Dictionary<string, object>(result.Metrics);
        
        // Add Unity-specific processing based on analysis type
        switch (analysisType)
        {
            case "ui":
                unityInsights.AddRange(ProcessUIInsights(result.Insights));
                unityMetrics["unity_ui_elements"] = CountUIElements(result.Insights);
                break;
                
            case "gameplay":
                unityInsights.AddRange(ProcessGameplayInsights(result.Insights));
                unityMetrics["unity_gameplay_score"] = CalculateGameplayScore(result.Insights);
                break;
                
            case "performance":
                unityInsights.AddRange(ProcessPerformanceInsights(result.Insights));
                unityMetrics["unity_performance_score"] = CalculatePerformanceScore(result.Insights);
                break;
                
            case "accessibility":
                unityInsights.AddRange(ProcessAccessibilityInsights(result.Insights));
                unityMetrics["unity_accessibility_score"] = CalculateAccessibilityScore(result.Insights);
                break;
        }

        return result with
        {
            Insights = result.Insights.Concat(unityInsights).ToList(),
            Metrics = unityMetrics,
            Summary = $"[Unity] {result.Summary}"
        };
    }

    private VisualComparisonResult PostProcessComparisonForUnity(VisualComparisonResult result, string comparisonType)
    {
        if (!result.Success)
            return result;

        // Add Unity-specific comparison insights
        var unityDifferences = new List<VisualDifference>();
        
        // Process differences for Unity-specific elements
        foreach (var difference in result.Differences)
        {
            var unityDifference = difference with
            {
                Description = $"[Unity] {difference.Description}",
                Metadata = new Dictionary<string, object>(difference.Metadata ?? new Dictionary<string, object>())
                {
                    ["unity_context"] = comparisonType,
                    ["analysis_timestamp"] = DateTime.UtcNow
                }
            };
            unityDifferences.Add(unityDifference);
        }

        return result with
        {
            Differences = unityDifferences,
            Summary = $"[Unity] {result.Summary}"
        };
    }
}
