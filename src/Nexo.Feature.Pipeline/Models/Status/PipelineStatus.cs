using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Result of pipeline configuration validation.
/// </summary>
public partial class PipelineValidationResult
{
    public bool IsValid { get; set; }
    
    public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
    
    public List<string> Warnings { get; set; } = new List<string>();
    
    public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Health status of the pipeline system.
/// </summary>
public partial class PipelineHealthStatus
{
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    
    public bool OverallHealth { get; set; }
    
    public Dictionary<string, bool> ComponentHealth { get; set; } = new Dictionary<string, bool>();
    
    public int ActiveExecutions { get; set; }
    
    public List<string> Issues { get; set; } = new List<string>();
    
    public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Metrics for pipeline execution.
/// </summary>
public partial class PipelineOrchestrationMetrics
{
    public string ExecutionId { get; set; } = string.Empty;
    
    public TimeSpan TotalExecutionTime { get; set; }
    
    public Dictionary<string, TimeSpan> StepExecutionTimes { get; set; } = new Dictionary<string, TimeSpan>();
    
    public Dictionary<string, long> MemoryUsage { get; set; } = new Dictionary<string, long>();
    
    public Dictionary<string, double> CpuUsage { get; set; } = new Dictionary<string, double>();
    
    public int TotalSteps { get; set; }
    
    public int SuccessfulSteps { get; set; }
    
    public int FailedSteps { get; set; }
    
    public double SuccessRate => TotalSteps > 0 ? (double)SuccessfulSteps / TotalSteps : 0;
}

/// <summary>
/// Component health information.
/// </summary>
public partial class ComponentHealth
{
    public bool IsHealthy { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime LastCheck { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
}
