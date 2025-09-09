using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models.Learning
{
    /// <summary>
    /// Represents optimization context for generating suggestions.
    /// </summary>
    public class OptimizationContext
    {
        public string Id { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Technology { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<string> Constraints { get; set; } = new List<string>();
        public Dictionary<string, object> Goals { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents performance data for analysis.
    /// </summary>
    public class PerformanceData
    {
        public string Id { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string Context { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents optimization report options.
    /// </summary>
    public class OptimizationReportOptions
    {
        public string ReportType { get; set; } = "comprehensive";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public List<string> Metrics { get; set; } = new List<string>();
        public bool IncludeRecommendations { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public string Format { get; set; } = "PDF";
    }

    /// <summary>
    /// Represents an optimization recommendation.
    /// </summary>
    public class OptimizationRecommendation
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public double Impact { get; set; }
        public double Effort { get; set; }
        public double Confidence { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of pattern analysis.
    /// </summary>
    public class PatternAnalysisResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PatternInsight> Insights { get; set; } = new List<PatternInsight>();
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();
        public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
        public DateTime AnalyzedAt { get; set; }
    }

    /// <summary>
    /// Represents a pattern insight.
    /// </summary>
    public class PatternInsight
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents optimization suggestions.
    /// </summary>
    public class OptimizationSuggestions
    {
        public string Id { get; set; } = string.Empty;
        public string ContextId { get; set; } = string.Empty;
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();
        public List<PatternInsight> Insights { get; set; } = new List<PatternInsight>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents performance recommendations.
    /// </summary>
    public class PerformanceRecommendations
    {
        public string Id { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents a performance recommendation.
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Id { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double TargetValue { get; set; }
        public double Improvement { get; set; }
        public string Priority { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new List<string>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents an optimization report.
    /// </summary>
    public class OptimizationReport
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object> Summary { get; set; } = new Dictionary<string, object>();
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();
        public List<PatternInsight> Insights { get; set; } = new List<PatternInsight>();
        public Dictionary<string, object> Charts { get; set; } = new Dictionary<string, object>();
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents feature optimization recommendations.
    /// </summary>
    public class FeatureOptimizationRecommendations
    {
        public string Id { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();
        public List<PerformanceRecommendation> PerformanceRecommendations { get; set; } = new List<PerformanceRecommendation>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of optimization validation.
    /// </summary>
    public class OptimizationValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<OptimizationRecommendation> ValidRecommendations { get; set; } = new List<OptimizationRecommendation>();
        public List<OptimizationRecommendation> InvalidRecommendations { get; set; } = new List<OptimizationRecommendation>();
        public DateTime ValidatedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of optimization application.
    /// </summary>
    public class OptimizationApplicationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AppliedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> AppliedRecommendations { get; set; } = new List<string>();
        public List<string> FailedRecommendations { get; set; } = new List<string>();
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public DateTime AppliedAt { get; set; }
    }

    /// <summary>
    /// Represents optimization metrics.
    /// </summary>
    public class OptimizationMetrics
    {
        public int TotalRecommendations { get; set; }
        public int AppliedRecommendations { get; set; }
        public int PendingRecommendations { get; set; }
        public double AverageImpact { get; set; }
        public double AverageEffort { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, object> CategoryMetrics { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }
}
