using Nexo.Agent.Tools.Visual.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Metrics calculation functionality for Unity visual analyzer.
/// </summary>
public sealed partial class UnityVisualAnalyzer
{
    private int CountUIElements(List<VisualInsight> insights)
    {
        return insights.Count(i => i.Category == "ui");
    }

    private double CalculateGameplayScore(List<VisualInsight> insights)
    {
        var positiveInsights = insights.Count(i => i.Type == "observation" && i.Severity == "low");
        var negativeInsights = insights.Count(i => i.Type == "issue" && i.Severity == "high");
        
        return Math.Max(0, Math.Min(1, (positiveInsights - negativeInsights) / 10.0));
    }

    private double CalculatePerformanceScore(List<VisualInsight> insights)
    {
        var performanceIssues = insights.Count(i => i.Category == "performance" && i.Severity == "high");
        return Math.Max(0, 1 - (performanceIssues * 0.2));
    }

    private double CalculateAccessibilityScore(List<VisualInsight> insights)
    {
        var accessibilityIssues = insights.Count(i => i.Category == "accessibility" && i.Severity == "high");
        return Math.Max(0, 1 - (accessibilityIssues * 0.3));
    }
}
