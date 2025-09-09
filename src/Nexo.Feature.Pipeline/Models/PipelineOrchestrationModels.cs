using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Request for executing a complete application development pipeline.
    /// </summary>
    public class ApplicationPipelineRequest
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
    public class AnalysisPipelineRequest
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
    public class PerformancePipelineRequest
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
    public class PlatformPipelineRequest
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

    /// <summary>
    /// Result of a complete application development pipeline execution.
    /// </summary>
    public class PipelineOrchestrationResult
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
    public class AnalysisPipelineResult
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
    public class PerformanceOptimizationResult
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
    public class PlatformIntegrationResult
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

    /// <summary>
    /// Result of pipeline configuration validation.
    /// </summary>
    public class PipelineValidationResult
    {
        public bool IsValid { get; set; }
        
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        
        public List<string> Warnings { get; set; } = new List<string>();
        
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Health status of the pipeline system.
    /// </summary>
    public class PipelineHealthStatus
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
    public class PipelineOrchestrationMetrics
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
    /// Template for pipeline configuration.
    /// </summary>
    public class PipelineTemplate
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

    /// <summary>
    /// Optimization action.
    /// </summary>
    public class OptimizationAction
    {
        public string ActionType { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Target { get; set; } = string.Empty;
        
        public bool WasApplied { get; set; }
        
        public double Improvement { get; set; }
    }

    /// <summary>
    /// Platform feature.
    /// </summary>
    public class PlatformFeature
    {
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public bool IsAvailable { get; set; }
        
        public string Version { get; set; } = string.Empty;
        
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Platform capability.
    /// </summary>
    public class PlatformCapability
    {
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Type { get; set; } = string.Empty;
        
        public bool IsAvailable { get; set; }
        
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Native API.
    /// </summary>
    public class NativeAPI
    {
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public bool IsAvailable { get; set; }
        
        public string Version { get; set; } = string.Empty;
        
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Validation issue.
    /// </summary>
    public class ValidationIssue
    {
        public string Field { get; set; } = string.Empty;
        
        public string Message { get; set; } = string.Empty;
        
        public string Severity { get; set; } = string.Empty;
        
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Component health information.
    /// </summary>
    public class ComponentHealth
    {
        public bool IsHealthy { get; set; }
        
        public string Status { get; set; } = string.Empty;
        
        public DateTime LastCheck { get; set; }
        
        public string Message { get; set; } = string.Empty;
        
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Type of pipeline execution.
    /// </summary>
    public enum PipelineType
    {
        ApplicationDevelopment,
        CodeAnalysis,
        PerformanceOptimization,
        PlatformIntegration
    }

    /// <summary>
    /// Type of analysis to perform.
    /// </summary>
    public enum AnalysisType
    {
        CodeQuality,
        Architecture,
        Performance,
        Security
    }

    /// <summary>
    /// Target for performance optimization.
    /// </summary>
    public enum OptimizationTarget
    {
        MemoryUsage,
        CpuUsage,
        NetworkUsage,
        BatteryLife
    }

    /// <summary>
    /// Mode for feature detection.
    /// </summary>
    public enum FeatureDetectionMode
    {
        Automatic,
        Manual,
        Selective
    }

    /// <summary>
    /// Optimization level for pipeline execution.
    /// </summary>
    public enum OptimizationLevel
    {
        None,
        Basic,
        Balanced,
        Aggressive,
        Maximum
    }

    /// <summary>
    /// Implementation complexity levels.
    /// </summary>
    public enum ImplementationComplexity
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Optimization types for performance improvements.
    /// </summary>
    public enum OptimizationType
    {
        CpuOptimization,
        MemoryOptimization,
        DiskOptimization,
        NetworkOptimization,
        AlgorithmOptimization,
        Parallelization,
        Caching,
        ResourceManagement
    }

    /// <summary>
    /// Optimization recommendation for pipeline performance.
    /// </summary>
    public class OptimizationRecommendation
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
} 