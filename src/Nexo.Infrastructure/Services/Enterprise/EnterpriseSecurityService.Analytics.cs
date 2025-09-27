using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Enterprise;
using Nexo.Core.Application.Models.Enterprise;
using Nexo.Feature.Enterprise.Interfaces;
using Nexo.Feature.Enterprise.Models;

namespace Nexo.Infrastructure.Services.Enterprise
{
    /// <summary>
    /// Security analytics functionality for EnterpriseSecurityService.
    /// Handles security analytics, monitoring, and insights generation.
    /// </summary>
    public partial class EnterpriseSecurityService
    {
        /// <summary>
        /// Analyzes security analytics based on AI analysis.
        /// </summary>
        public async Task<SecurityAnalyticsResult> AnalyzeSecurityAnalyticsAsync(
            SecurityAnalyticsRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Analyzing security analytics for {ProjectId}", request.ProjectId);

                var aiRequest = new SecurityAnalyticsAIRequest
                {
                    ProjectId = request.ProjectId,
                    AnalyticsScope = request.AnalyticsScope,
                    TimeRange = request.TimeRange,
                    AnalyticsMetrics = request.AnalyticsMetrics
                };

                var aiResponse = await _aiService.AnalyzeSecurityAnalyticsAsync(aiRequest, cancellationToken);

                var analyticsData = ParseAnalyticsData(aiResponse.Content);
                var analyticsMetrics = ParseAnalyticsMetrics(aiResponse.Content);

                return new SecurityAnalyticsResult
                {
                    Success = true,
                    Message = "Security analytics analyzed successfully",
                    AnalyticsData = analyticsData,
                    AnalyticsMetrics = analyticsMetrics,
                    AnalyzedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze security analytics for {ProjectId}", request.ProjectId);
                return new SecurityAnalyticsResult
                {
                    Success = false,
                    Message = ex.Message,
                    AnalyzedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }

        /// <summary>
        /// Monitors security events in real-time.
        /// </summary>
        public async Task<SecurityMonitoringResult> MonitorSecurityEventsAsync(
            SecurityMonitoringRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Monitoring security events for {ProjectId}", request.ProjectId);

                var aiRequest = new SecurityMonitoringAIRequest
                {
                    ProjectId = request.ProjectId,
                    MonitoringScope = request.MonitoringScope,
                    AlertThresholds = request.AlertThresholds,
                    MonitoringFrequency = request.MonitoringFrequency
                };

                var aiResponse = await _aiService.MonitorSecurityEventsAsync(aiRequest, cancellationToken);

                var securityEvents = ParseSecurityEvents(aiResponse.Content);
                var monitoringMetrics = ParseMonitoringMetrics(aiResponse.Content);

                return new SecurityMonitoringResult
                {
                    Success = true,
                    Message = "Security events monitored successfully",
                    SecurityEvents = securityEvents,
                    MonitoringMetrics = monitoringMetrics,
                    MonitoredAt = DateTimeOffset.UtcNow.DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor security events for {ProjectId}", request.ProjectId);
                return new SecurityMonitoringResult
                {
                    Success = false,
                    Message = ex.Message,
                    MonitoredAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }
    }
}
