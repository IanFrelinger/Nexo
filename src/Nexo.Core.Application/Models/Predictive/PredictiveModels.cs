using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models.Predictive
{
    /// <summary>
    /// Represents predictive analytics configuration.
    /// </summary>
    public class PredictiveAnalyticsConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AnalyticsTypes { get; set; } = new List<string>();
        public Dictionary<string, object> AnalyticsSettings { get; set; } = new Dictionary<string, object>();
        public List<string> DataSources { get; set; } = new List<string>();
        public Dictionary<string, object> PredictionSettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents complexity configuration for feature complexity prediction.
    /// </summary>
    public class ComplexityConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ComplexityFactors { get; set; } = new List<string>();
        public Dictionary<string, object> ComplexitySettings { get; set; } = new Dictionary<string, object>();
        public List<string> PredictionModels { get; set; } = new List<string>();
        public Dictionary<string, object> AccuracySettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents estimation configuration for development time estimation.
    /// </summary>
    public class EstimationConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> EstimationMethods { get; set; } = new List<string>();
        public Dictionary<string, object> EstimationSettings { get; set; } = new Dictionary<string, object>();
        public List<string> TimeFactors { get; set; } = new List<string>();
        public Dictionary<string, object> AccuracySettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents risk configuration for risk assessment.
    /// </summary>
    public class RiskConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RiskTypes { get; set; } = new List<string>();
        public Dictionary<string, object> RiskSettings { get; set; } = new Dictionary<string, object>();
        public List<string> AssessmentMethods { get; set; } = new List<string>();
        public Dictionary<string, object> MitigationSettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents predictive dashboard configuration.
    /// </summary>
    public class PredictiveDashboardConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> DashboardWidgets { get; set; } = new List<string>();
        public Dictionary<string, object> DashboardSettings { get; set; } = new Dictionary<string, object>();
        public List<string> DataSources { get; set; } = new List<string>();
        public Dictionary<string, object> DisplaySettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents recommendation configuration for predictive recommendations.
    /// </summary>
    public class RecommendationConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RecommendationTypes { get; set; } = new List<string>();
        public Dictionary<string, object> RecommendationSettings { get; set; } = new Dictionary<string, object>();
        public List<string> RecommendationSources { get; set; } = new List<string>();
        public Dictionary<string, object> PrioritySettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents report configuration for predictive development reports.
    /// </summary>
    public class ReportConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ReportTypes { get; set; } = new List<string>();
        public Dictionary<string, object> ReportSettings { get; set; } = new Dictionary<string, object>();
        public List<string> DataSources { get; set; } = new List<string>();
        public Dictionary<string, object> FormatSettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of predictive analytics implementation.
    /// </summary>
    public class PredictiveAnalyticsResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AnalyticsId { get; set; } = string.Empty;
        public List<string> ImplementedAnalytics { get; set; } = new List<string>();
        public Dictionary<string, object> AnalyticsMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime ImplementedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of complexity prediction.
    /// </summary>
    public class ComplexityPredictionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PredictionId { get; set; } = string.Empty;
        public double PredictedComplexity { get; set; }
        public string ComplexityLevel { get; set; } = string.Empty;
        public List<string> ComplexityFactors { get; set; } = new List<string>();
        public Dictionary<string, object> PredictionMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime PredictedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of time estimation.
    /// </summary>
    public class TimeEstimationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EstimationId { get; set; } = string.Empty;
        public TimeSpan EstimatedTime { get; set; }
        public TimeSpan ConfidenceInterval { get; set; }
        public List<string> TimeFactors { get; set; } = new List<string>();
        public Dictionary<string, object> EstimationMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime EstimatedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of risk assessment.
    /// </summary>
    public class RiskAssessmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AssessmentId { get; set; } = string.Empty;
        public double RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public List<string> IdentifiedRisks { get; set; } = new List<string>();
        public List<string> MitigationStrategies { get; set; } = new List<string>();
        public Dictionary<string, object> AssessmentMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime AssessedAt { get; set; }
    }

    /// <summary>
    /// Represents predictive development metrics.
    /// </summary>
    public class PredictiveDevelopmentMetrics
    {
        public double PredictionAccuracy { get; set; }
        public double ComplexityPredictionAccuracy { get; set; }
        public double TimeEstimationAccuracy { get; set; }
        public double RiskAssessmentAccuracy { get; set; }
        public int TotalPredictions { get; set; }
        public int SuccessfulPredictions { get; set; }
        public Dictionary<string, object> CategoryMetrics { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of predictive dashboard creation.
    /// </summary>
    public class PredictiveDashboardResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DashboardId { get; set; } = string.Empty;
        public List<string> CreatedDashboards { get; set; } = new List<string>();
        public Dictionary<string, object> DashboardMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of recommendation implementation.
    /// </summary>
    public class RecommendationImplementationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ImplementationId { get; set; } = string.Empty;
        public List<string> ImplementedRecommendations { get; set; } = new List<string>();
        public Dictionary<string, object> RecommendationMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime ImplementedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of report creation.
    /// </summary>
    public class ReportCreationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ReportId { get; set; } = string.Empty;
        public List<string> CreatedReports { get; set; } = new List<string>();
        public Dictionary<string, object> ReportMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }
}
