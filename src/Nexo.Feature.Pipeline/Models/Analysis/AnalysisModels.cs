using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Insight from code analysis.
/// </summary>
public class AnalysisInsight
{
    public string Type { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Severity { get; set; } = string.Empty;
    
    public string FilePath { get; set; } = string.Empty;
    
    public int? LineNumber { get; set; }
    
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Code quality metric.
/// </summary>
public class CodeQualityMetric
{
    public string MetricName { get; set; } = string.Empty;
    
    public double Value { get; set; }
    
    public string Unit { get; set; } = string.Empty;
    
    public string Threshold { get; set; } = string.Empty;
    
    public bool IsWithinThreshold { get; set; }
}

/// <summary>
/// Security vulnerability.
/// </summary>
public class SecurityVulnerability
{
    public string VulnerabilityType { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Severity { get; set; } = string.Empty;
    
    public string FilePath { get; set; } = string.Empty;
    
    public int? LineNumber { get; set; }
    
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Performance recommendation.
/// </summary>
public class PerformanceRecommendation
{
    public string Category { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Impact { get; set; } = string.Empty;
    
    public string Implementation { get; set; } = string.Empty;
    
    public double EstimatedImprovement { get; set; }
}

/// <summary>
/// Test recommendation.
/// </summary>
public class TestRecommendation
{
    public string TestType { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string TargetComponent { get; set; } = string.Empty;
    
    public string Priority { get; set; } = string.Empty;
    
    public string Implementation { get; set; } = string.Empty;
}
