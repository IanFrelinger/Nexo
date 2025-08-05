using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Models;

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