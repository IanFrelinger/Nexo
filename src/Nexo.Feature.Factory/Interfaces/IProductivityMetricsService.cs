using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Service for measuring, tracking, and validating productivity improvements in the Feature Factory
/// </summary>
public interface IProductivityMetricsService
{
    /// <summary>
    /// Tracks development time for a feature generation process
    /// </summary>
    /// <param name="trackingRequest">Development time tracking request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tracking result</returns>
    Task<DevelopmentTimeTrackingResult> TrackDevelopmentTimeAsync(DevelopmentTimeTrackingRequest trackingRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates productivity metrics for a specific time period
    /// </summary>
    /// <param name="metricsRequest">Productivity metrics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Productivity metrics</returns>
    Task<ProductivityMetricsResult> CalculateProductivityMetricsAsync(ProductivityMetricsRequest metricsRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates 32× productivity improvement claims
    /// </summary>
    /// <param name="validationRequest">Productivity validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ProductivityValidationResult> ValidateProductivityImprovementAsync(ProductivityValidationRequest validationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feature delivery metrics
    /// </summary>
    /// <param name="deliveryRequest">Feature delivery metrics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feature delivery metrics</returns>
    Task<FeatureDeliveryMetrics> GetFeatureDeliveryMetricsAsync(FeatureDeliveryMetricsRequest deliveryRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates productivity dashboard data
    /// </summary>
    /// <param name="dashboardRequest">Dashboard request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Productivity dashboard data</returns>
    Task<ProductivityDashboardData> GetProductivityDashboardAsync(ProductivityDashboardRequest dashboardRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares traditional vs Feature Factory development metrics
    /// </summary>
    /// <param name="comparisonRequest">Comparison request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Development comparison metrics</returns>
    Task<DevelopmentComparisonMetrics> CompareDevelopmentApproachesAsync(DevelopmentComparisonRequest comparisonRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets productivity trends over time
    /// </summary>
    /// <param name="trendsRequest">Trends request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Productivity trends</returns>
    Task<ProductivityTrendsResult> GetProductivityTrendsAsync(ProductivityTrendsRequest trendsRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports productivity reports
    /// </summary>
    /// <param name="exportRequest">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<ProductivityExportResult> ExportProductivityReportAsync(ProductivityExportRequest exportRequest, CancellationToken cancellationToken = default);
}

/// <summary>
/// Development time tracking request
/// </summary>
public record DevelopmentTimeTrackingRequest
{
    public string FeatureId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public DevelopmentApproach Approach { get; init; }
    public List<DevelopmentPhase> Phases { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Development approach types
/// </summary>
public enum DevelopmentApproach
{
    Traditional,
    FeatureFactory,
    Hybrid
}

/// <summary>
/// Development phase tracking
/// </summary>
public record DevelopmentPhase
{
    public string PhaseName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, object> PhaseData { get; init; } = new();
}

/// <summary>
/// Development time tracking result
/// </summary>
public record DevelopmentTimeTrackingResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public TimeSpan TotalDevelopmentTime { get; init; }
    public List<DevelopmentPhase> Phases { get; init; } = new();
    public DateTime TrackedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Productivity metrics request
/// </summary>
public record ProductivityMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string? UserId { get; init; }
    public List<string> Metrics { get; init; } = new();
    public string GroupBy { get; init; } = string.Empty;
}

/// <summary>
/// Productivity metrics result
/// </summary>
public record ProductivityMetricsResult
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double ProductivityMultiplier { get; init; }
    public TimeSpan AverageTraditionalTime { get; init; }
    public TimeSpan AverageFeatureFactoryTime { get; init; }
    public double TimeSavingsPercentage { get; init; }
    public double CostSavingsPercentage { get; init; }
    public int TotalFeatures { get; init; }
    public int TraditionalFeatures { get; init; }
    public int FeatureFactoryFeatures { get; init; }
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public List<ProductivityDataPoint> DataPoints { get; init; } = new();
}

/// <summary>
/// Productivity data point
/// </summary>
public record ProductivityDataPoint
{
    public DateTime Date { get; init; }
    public double ProductivityMultiplier { get; init; }
    public TimeSpan AverageTime { get; init; }
    public int FeatureCount { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// Productivity validation request
/// </summary>
public record ProductivityValidationRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double TargetMultiplier { get; init; } = 32.0;
    public string? ProjectId { get; init; }
    public List<string> ValidationCriteria { get; init; } = new();
}

/// <summary>
/// Productivity validation result
/// </summary>
public record ProductivityValidationResult
{
    public bool IsValidated { get; init; }
    public double AchievedMultiplier { get; init; }
    public double TargetMultiplier { get; init; }
    public double ValidationScore { get; init; }
    public List<ValidationCriterion> Criteria { get; init; } = new();
    public List<string> ValidationIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Validation criterion
/// </summary>
public record ValidationCriterion
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsMet { get; init; }
    public double Score { get; init; }
    public string? Issue { get; init; }
}

/// <summary>
/// Feature delivery metrics request
/// </summary>
public record FeatureDeliveryMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string? TeamId { get; init; }
    public List<string> Metrics { get; init; } = new();
}

/// <summary>
/// Feature delivery metrics
/// </summary>
public record FeatureDeliveryMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int FeaturesDelivered { get; init; }
    public int FeaturesPerDay { get; init; }
    public int FeaturesPerWeek { get; init; }
    public int FeaturesPerMonth { get; init; }
    public TimeSpan AverageDeliveryTime { get; init; }
    public double OnTimeDeliveryRate { get; init; }
    public double QualityScore { get; init; }
    public Dictionary<string, int> FeaturesByType { get; init; } = new();
    public Dictionary<string, int> FeaturesByPriority { get; init; } = new();
    public List<DeliveryTrend> Trends { get; init; } = new();
}

/// <summary>
/// Delivery trend
/// </summary>
public record DeliveryTrend
{
    public DateTime Date { get; init; }
    public int FeaturesDelivered { get; init; }
    public TimeSpan AverageTime { get; init; }
    public double QualityScore { get; init; }
}

/// <summary>
/// Productivity dashboard request
/// </summary>
public record ProductivityDashboardRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> Widgets { get; init; } = new();
    public string RefreshInterval { get; init; } = string.Empty;
}

/// <summary>
/// Productivity dashboard data
/// </summary>
public record ProductivityDashboardData
{
    public DateTime GeneratedAt { get; init; }
    public ProductivitySummary Summary { get; init; } = new();
    public List<DashboardWidget> Widgets { get; init; } = new();
    public List<Alert> Alerts { get; init; } = new();
    public Dictionary<string, object> CustomData { get; init; } = new();
}

/// <summary>
/// Productivity summary
/// </summary>
public record ProductivitySummary
{
    public double CurrentProductivityMultiplier { get; init; }
    public int FeaturesThisWeek { get; init; }
    public int FeaturesThisMonth { get; init; }
    public double TimeSavingsThisWeek { get; init; }
    public double CostSavingsThisMonth { get; init; }
    public string Trend { get; init; } = string.Empty;
}

/// <summary>
/// Dashboard widget
/// </summary>
public record DashboardWidget
{
    public string WidgetId { get; init; } = string.Empty;
    public string WidgetType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Alert information
/// </summary>
public record Alert
{
    public string AlertId { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime RaisedAt { get; init; }
    public bool IsAcknowledged { get; init; }
}

/// <summary>
/// Development comparison request
/// </summary>
public record DevelopmentComparisonRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> ComparisonMetrics { get; init; } = new();
}

/// <summary>
/// Development comparison metrics
/// </summary>
public record DevelopmentComparisonMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public TraditionalDevelopmentMetrics Traditional { get; init; } = new();
    public FeatureFactoryMetrics FeatureFactory { get; init; } = new();
    public ComparisonSummary Summary { get; init; } = new();
    public List<ComparisonDetail> Details { get; init; } = new();
}

/// <summary>
/// Traditional development metrics
/// </summary>
public record TraditionalDevelopmentMetrics
{
    public int FeatureCount { get; init; }
    public TimeSpan AverageDevelopmentTime { get; init; }
    public double AverageCost { get; init; }
    public double QualityScore { get; init; }
    public double SuccessRate { get; init; }
    public List<string> CommonIssues { get; init; } = new();
}

/// <summary>
/// Feature Factory metrics
/// </summary>
public record FeatureFactoryMetrics
{
    public int FeatureCount { get; init; }
    public TimeSpan AverageDevelopmentTime { get; init; }
    public double AverageCost { get; init; }
    public double QualityScore { get; init; }
    public double SuccessRate { get; init; }
    public List<string> Advantages { get; init; } = new();
}

/// <summary>
/// Comparison summary
/// </summary>
public record ComparisonSummary
{
    public double ProductivityImprovement { get; init; }
    public double TimeSavings { get; init; }
    public double CostSavings { get; init; }
    public double QualityImprovement { get; init; }
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Comparison detail
/// </summary>
public record ComparisonDetail
{
    public string MetricName { get; init; } = string.Empty;
    public double TraditionalValue { get; init; }
    public double FeatureFactoryValue { get; init; }
    public double Improvement { get; init; }
    public string Unit { get; init; } = string.Empty;
}

/// <summary>
/// Productivity trends request
/// </summary>
public record ProductivityTrendsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string Granularity { get; init; } = string.Empty;
    public List<string> Metrics { get; init; } = new();
}

/// <summary>
/// Productivity trends result
/// </summary>
public record ProductivityTrendsResult
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Granularity { get; init; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public TrendAnalysis Analysis { get; init; } = new();
    public List<TrendForecast> Forecasts { get; init; } = new();
}

/// <summary>
/// Trend data point
/// </summary>
public record TrendDataPoint
{
    public DateTime Date { get; init; }
    public double ProductivityMultiplier { get; init; }
    public int FeatureCount { get; init; }
    public TimeSpan AverageTime { get; init; }
    public double QualityScore { get; init; }
    public double CostSavings { get; init; }
}

/// <summary>
/// Trend analysis
/// </summary>
public record TrendAnalysis
{
    public string OverallTrend { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public List<string> KeyInsights { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Trend forecast
/// </summary>
public record TrendForecast
{
    public DateTime ForecastDate { get; init; }
    public double PredictedProductivityMultiplier { get; init; }
    public double ConfidenceLevel { get; init; }
    public string ForecastMethod { get; init; } = string.Empty;
}

/// <summary>
/// Productivity export request
/// </summary>
public record ProductivityExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> Metrics { get; init; } = new();
    public string? FilePath { get; init; }
    public bool IncludeCharts { get; init; }
}

/// <summary>
/// Productivity export result
/// </summary>
public record ProductivityExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 