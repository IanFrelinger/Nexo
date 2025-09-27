using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.BetaTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Enums.BetaTesting;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// Analytics and reporting functionality
    /// </summary>
    public partial class BetaTestingProgram
    {
        /// <summary>
        /// Generates analytics report for the beta program
        /// </summary>
        public async Task<BetaAnalyticsReport> GenerateAnalyticsReportAsync(string programId, AnalyticsReportRequest request)
        {
            _logger.LogInformation("Generating analytics report for program: {ProgramId}", programId);

            try
            {
                var program = await GetProgramAsync(programId);
                if (program == null)
                {
                    throw new InvalidOperationException($"Program {programId} not found");
                }

                // Collect analytics data
                var userMetricsData = await _analytics.GetUserMetricsAsync(programId);
                var engagementMetricsData = await _analytics.GetEngagementMetricsAsync(programId);
                var feedbackMetricsData = await _analytics.GetFeedbackMetricsAsync(programId);
                var performanceMetricsData = await _analytics.GetPerformanceMetricsAsync(programId);
                
                // Convert to proper metric objects
                var userMetrics = new UserMetrics
                {
                    TotalUsers = userMetricsData.ContainsKey("TotalUsers") ? Convert.ToInt32(userMetricsData["TotalUsers"]) : 0,
                    ActiveUsers = userMetricsData.ContainsKey("ActiveUsers") ? Convert.ToInt32(userMetricsData["ActiveUsers"]) : 0,
                    RetentionRate = userMetricsData.ContainsKey("RetentionRate") ? Convert.ToDouble(userMetricsData["RetentionRate"]) : 0.0,
                    ChurnRate = userMetricsData.ContainsKey("ChurnRate") ? Convert.ToDouble(userMetricsData["ChurnRate"]) : 0.0,
                    Demographics = userMetricsData
                };
                
                var engagementMetrics = new EngagementMetrics
                {
                    TotalSessions = engagementMetricsData.ContainsKey("TotalSessions") ? Convert.ToInt32(engagementMetricsData["TotalSessions"]) : 0,
                    FeatureUsageRate = engagementMetricsData.ContainsKey("FeatureUsageRate") ? Convert.ToDouble(engagementMetricsData["FeatureUsageRate"]) : 0.0,
                    PageViews = engagementMetricsData.ContainsKey("PageViews") ? Convert.ToInt32(engagementMetricsData["PageViews"]) : 0,
                    BounceRate = engagementMetricsData.ContainsKey("BounceRate") ? Convert.ToDouble(engagementMetricsData["BounceRate"]) : 0.0
                };
                
                var feedbackMetrics = new FeedbackMetrics
                {
                    TotalFeedback = feedbackMetricsData.ContainsKey("TotalFeedback") ? Convert.ToInt32(feedbackMetricsData["TotalFeedback"]) : 0,
                    AverageRating = feedbackMetricsData.ContainsKey("AverageRating") ? Convert.ToDouble(feedbackMetricsData["AverageRating"]) : 0.0,
                    ResponseRate = feedbackMetricsData.ContainsKey("ResponseRate") ? Convert.ToDouble(feedbackMetricsData["ResponseRate"]) : 0.0
                };
                
                var performanceMetrics = new PerformanceMetrics
                {
                    AverageResponseTime = performanceMetricsData.ContainsKey("AverageResponseTime") ? Convert.ToDouble(performanceMetricsData["AverageResponseTime"]) : 0.0,
                    ErrorRate = performanceMetricsData.ContainsKey("ErrorRate") ? Convert.ToDouble(performanceMetricsData["ErrorRate"]) : 0.0,
                    Throughput = performanceMetricsData.ContainsKey("Throughput") ? Convert.ToDouble(performanceMetricsData["Throughput"]) : 0.0
                };

                var report = new BetaAnalyticsReport
                {
                    ProgramId = programId,
                    GeneratedAt = DateTime.UtcNow,
                    DateRange = request.DateRange,
                    UserMetrics = userMetrics,
                    EngagementMetrics = engagementMetrics,
                    FeedbackMetrics = feedbackMetrics,
                    PerformanceMetrics = performanceMetrics,
                    Recommendations = await GenerateRecommendationsAsync(program, userMetrics, engagementMetrics, feedbackMetrics)
                };

                _logger.LogInformation("Analytics report generated for program: {ProgramId}", programId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate analytics report for program {ProgramId}", programId);
                throw;
            }
        }
    }
}
