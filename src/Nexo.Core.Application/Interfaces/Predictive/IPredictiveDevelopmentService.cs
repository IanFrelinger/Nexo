using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Predictive;

namespace Nexo.Core.Application.Interfaces.Predictive
{
    /// <summary>
    /// Interface for predictive development service in Phase 9.
    /// Provides predictive analytics for feature development with complexity prediction and risk assessment.
    /// </summary>
    public interface IPredictiveDevelopmentService
    {
        /// <summary>
        /// Implements predictive analytics for feature development.
        /// </summary>
        /// <param name="analyticsConfig">The analytics configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Predictive analytics result</returns>
        Task<PredictiveAnalyticsResult> ImplementPredictiveAnalyticsAsync(
            PredictiveAnalyticsConfiguration analyticsConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates feature complexity prediction.
        /// </summary>
        /// <param name="complexityConfig">The complexity configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Complexity prediction result</returns>
        Task<ComplexityPredictionResult> CreateFeatureComplexityPredictionAsync(
            ComplexityConfiguration complexityConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds development time estimation.
        /// </summary>
        /// <param name="estimationConfig">The estimation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Time estimation result</returns>
        Task<TimeEstimationResult> AddDevelopmentTimeEstimationAsync(
            EstimationConfiguration estimationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates risk assessment capabilities.
        /// </summary>
        /// <param name="riskConfig">The risk configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Risk assessment result</returns>
        Task<RiskAssessmentResult> CreateRiskAssessmentCapabilitiesAsync(
            RiskConfiguration riskConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets predictive development metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Predictive development metrics</returns>
        Task<PredictiveDevelopmentMetrics> GetPredictiveDevelopmentMetricsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates predictive development dashboard.
        /// </summary>
        /// <param name="dashboardConfig">The dashboard configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dashboard creation result</returns>
        Task<PredictiveDashboardResult> CreatePredictiveDevelopmentDashboardAsync(
            PredictiveDashboardConfiguration dashboardConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements predictive recommendations.
        /// </summary>
        /// <param name="recommendationConfig">The recommendation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recommendation implementation result</returns>
        Task<RecommendationImplementationResult> ImplementPredictiveRecommendationsAsync(
            RecommendationConfiguration recommendationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates predictive development reports.
        /// </summary>
        /// <param name="reportConfig">The report configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Report creation result</returns>
        Task<ReportCreationResult> CreatePredictiveDevelopmentReportsAsync(
            ReportConfiguration reportConfig,
            CancellationToken cancellationToken = default);
    }
}
