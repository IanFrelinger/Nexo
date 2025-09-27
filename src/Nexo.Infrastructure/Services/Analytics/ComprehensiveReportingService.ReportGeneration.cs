using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Analytics;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Report generation functionality for comprehensive reporting.
    /// </summary>
    public partial class ComprehensiveReportingService
    {
        public async Task<UsageReport> GenerateUsageReportAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating usage report for period {StartTime} to {EndTime}", startTime, endTime);

                // Generate AI analytics to get usage data
                var aiAnalytics = await _aiAnalyticsService.GetUsageAnalyticsAsync(startTime, endTime, cancellationToken);

                return new UsageReport
                {
                    TotalEvents = aiAnalytics.TotalEvents,
                    UniqueUsers = aiAnalytics.UniqueUsers,
                    TotalTokens = aiAnalytics.TotalTokens,
                    AverageResponseTime = aiAnalytics.AverageResponseTime,
                    SuccessRate = aiAnalytics.SuccessRate,
                    EventsByType = aiAnalytics.EventsByType,
                    EventsByModel = aiAnalytics.EventsByModel,
                    TopUsers = aiAnalytics.TopUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating usage report");
                throw;
            }
        }

        public async Task<PerformanceReport> GeneratePerformanceReportAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating performance report for period {StartTime} to {EndTime}", startTime, endTime);

                // Generate AI analytics to get performance data
                var aiAnalytics = await _aiAnalyticsService.GetPerformanceAnalyticsAsync(startTime, endTime, cancellationToken);

                return new PerformanceReport
                {
                    TotalMetrics = aiAnalytics.TotalMetrics,
                    AverageLatency = aiAnalytics.AverageLatency,
                    AverageThroughput = aiAnalytics.AverageThroughput,
                    AverageAccuracy = aiAnalytics.AverageAccuracy,
                    ErrorRate = aiAnalytics.ErrorRate,
                    ResourceUtilization = aiAnalytics.ResourceUtilization,
                    PerformanceTrends = aiAnalytics.PerformanceTrends,
                    Bottlenecks = aiAnalytics.Bottlenecks
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance report");
                throw;
            }
        }

        public async Task<SecurityReport> GenerateSecurityReportAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating security report for period {StartTime} to {EndTime}", startTime, endTime);

                // Generate security compliance data
                var securityCompliance = await _securityComplianceService.GenerateComplianceReportAsync(startTime, endTime, cancellationToken);

                return new SecurityReport
                {
                    TotalEvents = securityCompliance.TotalEvents,
                    FailedAttempts = securityCompliance.SecurityMetrics.FailedAuthenticationAttempts,
                    SuccessRate = securityCompliance.SecurityMetrics.SuccessfulAuthenticationAttempts / (double)Math.Max(1, securityCompliance.SecurityMetrics.SuccessfulAuthenticationAttempts + securityCompliance.SecurityMetrics.FailedAuthenticationAttempts) * 100,
                    AverageResponseTime = securityCompliance.SecurityMetrics.AverageResponseTime,
                    SecurityScore = securityCompliance.ComplianceMetrics.ComplianceScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating security report");
                throw;
            }
        }

        public async Task<CostReport> GenerateCostReportAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating cost report for period {StartTime} to {EndTime}", startTime, endTime);

                // Generate AI analytics to get usage data for cost calculation
                var aiAnalytics = await _aiAnalyticsService.GetUsageAnalyticsAsync(startTime, endTime, cancellationToken);

                return new CostReport
                {
                    TotalCost = aiAnalytics.TotalCost,
                    TotalTokens = aiAnalytics.TotalTokens,
                    CostPerToken = aiAnalytics.TotalTokens > 0 ? aiAnalytics.TotalCost / aiAnalytics.TotalTokens : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cost report");
                throw;
            }
        }
    }
}
