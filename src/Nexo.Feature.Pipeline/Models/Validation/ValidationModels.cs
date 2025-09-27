using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Validation issue.
/// </summary>
public partial class ValidationIssue
{
    public string Field { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string Severity { get; set; } = string.Empty;
    
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Optimization action.
/// </summary>
public partial class OptimizationAction
{
    public string ActionType { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Target { get; set; } = string.Empty;
    
    public bool WasApplied { get; set; }
    
    public double Improvement { get; set; }
}

/// <summary>
/// Template for pipeline configuration.
/// </summary>
public partial class PipelineTemplate
{
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;
    
    public Dictionary<string, object> DefaultConfiguration { get; set; } = new Dictionary<string, object>();
    
    public List<string> RequiredParameters { get; set; } = new List<string>();
    
    public List<string> OptionalParameters { get; set; } = new List<string>();
    
    public TimeSpan EstimatedDuration { get; set; }
    
    public List<string> Tags { get; set; } = new List<string>();
}

/// <summary>
/// Optimization recommendation for pipeline performance.
/// </summary>
public partial class OptimizationRecommendation
{
    /// <summary>
    /// Gets or sets the recommendation identifier.
    /// </summary>
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the recommendation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected performance gain.
    /// </summary>
    public double ExpectedPerformanceGain { get; set; }

    /// <summary>
    /// Gets or sets the recommendation details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
}
