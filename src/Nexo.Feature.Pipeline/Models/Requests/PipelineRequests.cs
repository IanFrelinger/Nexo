using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Request for executing a complete application development pipeline.
/// </summary>
public partial class ApplicationPipelineRequest
{
    [Required]
    public string ApplicationName { get; set; } = string.Empty;
    
    [Required]
    public string SourceCode { get; set; } = string.Empty;
    
    public string TargetPlatform { get; set; } = "dotnet";
    
    public PipelineType PipelineType { get; set; } = PipelineType.ApplicationDevelopment;
    
    public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    
    public bool EnableTesting { get; set; } = true;
    
    public bool EnablePerformanceOptimization { get; set; } = true;
    
    public bool EnablePlatformIntegration { get; set; } = true;
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Request for executing an analysis pipeline on existing code.
/// </summary>
public partial class AnalysisPipelineRequest
{
    [Required]
    public string SourceCode { get; set; } = string.Empty;
    
    public AnalysisType AnalysisType { get; set; } = AnalysisType.CodeQuality;
    
    public string TargetPlatform { get; set; } = "dotnet";
    
    public bool IncludeGitAnalysis { get; set; } = true;
    
    public bool GenerateTestRecommendations { get; set; } = true;
    
    public Dictionary<string, object> AnalysisConfiguration { get; set; } = new Dictionary<string, object>();
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Request for executing a performance optimization pipeline.
/// </summary>
public partial class PerformancePipelineRequest
{
    [Required]
    public string ApplicationName { get; set; } = string.Empty;
    
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();
    
    public OptimizationTarget OptimizationTarget { get; set; } = OptimizationTarget.MemoryUsage;
    
    public bool EnableProfiling { get; set; } = true;
    
    public bool GenerateOptimizationReport { get; set; } = true;
    
    public Dictionary<string, object> OptimizationConfiguration { get; set; } = new Dictionary<string, object>();
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(20);
}

/// <summary>
/// Request for executing a platform integration pipeline.
/// </summary>
public partial class PlatformPipelineRequest
{
    [Required]
    public string PlatformName { get; set; } = string.Empty;
    
    public string PlatformVersion { get; set; } = "8.0";
    
    public FeatureDetectionMode FeatureDetectionMode { get; set; } = FeatureDetectionMode.Automatic;
    
    public bool EnableNativeAPIIntegration { get; set; } = true;
    
    public bool GeneratePlatformSpecificCode { get; set; } = true;
    
    public Dictionary<string, object> PlatformConfiguration { get; set; } = new Dictionary<string, object>();
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
}
