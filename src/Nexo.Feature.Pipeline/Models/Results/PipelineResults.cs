using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Result of a complete application development pipeline execution.
/// </summary>
public partial class PipelineOrchestrationResult
{
    public string ExecutionId { get; set; } = string.Empty;
    
    public PipelineExecutionStatus Status { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    
    public bool IsSuccess => Status == PipelineExecutionStatus.Completed;
    
    public List<PipelineExecutionStep> ExecutionSteps { get; set; } = new List<PipelineExecutionStep>();
    
    public Dictionary<string, object> Artifacts { get; set; } = new Dictionary<string, object>();
    
    public List<string> Warnings { get; set; } = new List<string>();
    
    public List<string> Errors { get; set; } = new List<string>();
    
    public PipelineOrchestrationMetrics Metrics { get; set; } = new PipelineOrchestrationMetrics();
    
    public string GeneratedCodePath { get; set; } = string.Empty;
    
    public string TestSuitePath { get; set; } = string.Empty;
    
    public string DocumentationPath { get; set; } = string.Empty;
}

/// <summary>
/// Result of an analysis pipeline execution.
/// </summary>
public partial class AnalysisPipelineResult
{
    public string ExecutionId { get; set; } = string.Empty;
    
    public PipelineExecutionStatus Status { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    
    public bool IsSuccess => Status == PipelineExecutionStatus.Completed;
    
    public List<AnalysisInsight> Insights { get; set; } = new List<AnalysisInsight>();
    
    public List<CodeQualityMetric> QualityMetrics { get; set; } = new List<CodeQualityMetric>();
    
    public List<SecurityVulnerability> SecurityIssues { get; set; } = new List<SecurityVulnerability>();
    
    public List<PerformanceRecommendation> PerformanceRecommendations { get; set; } = new List<PerformanceRecommendation>();
    
    public List<TestRecommendation> TestRecommendations { get; set; } = new List<TestRecommendation>();
    
    public string AnalysisReportPath { get; set; } = string.Empty;
    
    public List<string> Warnings { get; set; } = new List<string>();
    
    public List<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Result of a performance optimization pipeline execution.
/// </summary>
public partial class PerformanceOptimizationResult
{
    public string ExecutionId { get; set; } = string.Empty;
    
    public PipelineExecutionStatus Status { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    
    public bool IsSuccess => Status == PipelineExecutionStatus.Completed;
    
    public Dictionary<string, double> BeforeMetrics { get; set; } = new Dictionary<string, double>();
    
    public Dictionary<string, double> AfterMetrics { get; set; } = new Dictionary<string, double>();
    
    public Dictionary<string, double> Improvements { get; set; } = new Dictionary<string, double>();
    
    public List<OptimizationAction> AppliedOptimizations { get; set; } = new List<OptimizationAction>();
    
    public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
    
    public string OptimizationReportPath { get; set; } = string.Empty;
    
    public List<string> Warnings { get; set; } = new List<string>();
    
    public List<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Result of a platform integration pipeline execution.
/// </summary>
public partial class PlatformIntegrationResult
{
    public string ExecutionId { get; set; } = string.Empty;
    
    public PipelineExecutionStatus Status { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    
    public bool IsSuccess => Status == PipelineExecutionStatus.Completed;
    
    public List<PlatformFeature> DetectedFeatures { get; set; } = new List<PlatformFeature>();
    
    public List<PlatformCapability> Capabilities { get; set; } = new List<PlatformCapability>();
    
    public List<NativeAPI> AvailableAPIs { get; set; } = new List<NativeAPI>();
    
    public string GeneratedIntegrationCodePath { get; set; } = string.Empty;
    
    public List<string> Warnings { get; set; } = new List<string>();
    
    public List<string> Errors { get; set; } = new List<string>();
}
