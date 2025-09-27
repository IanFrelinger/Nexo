using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Services.AI.Analytics
{
    /// <summary>
    /// Analytics request
    /// </summary>
    public partial class AnalyticsRequest
    {
        public TimeSpan? TimeRange { get; set; }
        public string? UserId { get; set; }
        public List<string> Metrics { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Advanced analytics result
    /// </summary>
    public partial class AdvancedAnalyticsResult
    {
        public AnalyticsRequest Request { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public List<AnalyticsInsight> Insights { get; set; } = new();
        public List<AnalyticsPrediction> Predictions { get; set; } = new();
        public List<AnalyticsRecommendation> Recommendations { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
        public List<UsagePattern> UsagePatterns { get; set; } = new();
        public List<AnomalyDetection> Anomalies { get; set; } = new();
        public TrendAnalysis TrendAnalysis { get; set; } = new();
    }

    /// <summary>
    /// Analytics insight
    /// </summary>
    public partial class AnalyticsInsight
    {
        public InsightType Type { get; set; }
        public InsightSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Analytics prediction
    /// </summary>
    public partial class AnalyticsPrediction
    {
        public PredictionType Type { get; set; }
        public TimeSpan TimeHorizon { get; set; }
        public double PredictedValue { get; set; }
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
    }

    /// <summary>
    /// Analytics recommendation
    /// </summary>
    public partial class AnalyticsRecommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public string ExpectedImpact { get; set; } = string.Empty;
        public string ImplementationEffort { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public partial class PerformanceMetrics
    {
        public double SuccessRate { get; set; }
        public TimeSpan AverageOperationDuration { get; set; }
        public int TotalOperations { get; set; }
        public int FailedOperations { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
        public double ResourceUtilization { get; set; }
        public double QualityScore { get; set; }
    }

    /// <summary>
    /// Usage pattern
    /// </summary>
    public partial class UsagePattern
    {
        public PatternType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Anomaly detection
    /// </summary>
    public partial class AnomalyDetection
    {
        public AnomalyType Type { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public double Confidence { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Trend analysis
    /// </summary>
    public partial class TrendAnalysis
    {
        public TrendDirection UsageTrend { get; set; }
        public TrendDirection PerformanceTrend { get; set; }
        public TrendDirection ErrorTrend { get; set; }
        public TrendDirection ResourceTrend { get; set; }
        public TrendDirection OverallTrend { get; set; }
    }

    /// <summary>
    /// Analytics model
    /// </summary>
    public partial class AnalyticsModel
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public ModelStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TrainingCompletedAt { get; set; }
        public double Accuracy { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<FineTuningSample> TrainingData { get; set; } = new();
    }

    /// <summary>
    /// Model training request
    /// </summary>
    public partial class ModelTrainingRequest
    {
        public string ModelName { get; set; } = string.Empty;
        public ModelType ModelType { get; set; }
        public List<FineTuningSample> TrainingData { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Model training result
    /// </summary>
    public partial class ModelTrainingResult
    {
        public string ModelId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan TrainingDuration { get; set; }
        public double Accuracy { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    // Enums
    public enum InsightType { Performance, Usage, Efficiency, Quality, Security }
    public enum InsightSeverity { Low, Medium, High, Critical }
    public enum PredictionType { Usage, Performance, Resource, Quality, Cost }
    public enum RecommendationType { Performance, Scalability, Efficiency, Security, Cost }
    public enum RecommendationPriority { Low, Medium, High, Critical }
    public enum PatternType { Temporal, Operational, Behavioral, Resource }
    public enum AnomalyType { ErrorSpike, UsageSpike, PerformanceDrop, SecurityBreach }
    public enum AnomalySeverity { Low, Medium, High, Critical }
    public enum TrendDirection { Increasing, Decreasing, Stable, Volatile }
    public enum ModelType { Classification, Regression, Clustering, AnomalyDetection }
    public enum ModelStatus { Training, Trained, Failed, Deployed }
}
