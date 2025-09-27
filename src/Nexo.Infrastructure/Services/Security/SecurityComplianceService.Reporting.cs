using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Reporting functionality for security compliance service.
    /// </summary>
    public partial class SecurityComplianceService
    {
        /// <summary>
        /// Generates a comprehensive security compliance report.
        /// </summary>
        public async Task<SecurityComplianceReport> GenerateComplianceReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default)
        {
            var auditReport = await _auditLogger.GenerateAuditReportAsync(startTime, endTime, cancellationToken);
            var apiKeyStats = await _apiKeyManager.GetUsageStatisticsAsync(cancellationToken);
            var apiKeys = await _apiKeyManager.ListApiKeysAsync(cancellationToken);

            var report = new SecurityComplianceReport
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                StartTime = startTime,
                EndTime = endTime,
                TotalEvents = auditReport.TotalEvents,
                SecurityEvents = auditReport.SecurityEvents,
                ComplianceEvents = auditReport.ComplianceEvents,
                ApiKeyStatistics = apiKeyStats,
                SecurityMetrics = CalculateSecurityMetrics(auditReport, apiKeyStats),
                ComplianceMetrics = CalculateComplianceMetrics(auditReport),
                Recommendations = await GenerateSecurityRecommendationsAsync(auditReport, apiKeyStats, cancellationToken),
                Violations = await IdentifyComplianceViolationsAsync(auditReport, apiKeys, cancellationToken)
            };

            return report;
        }

        /// <summary>
        /// Performs a security health check.
        /// </summary>
        public async Task<SecurityHealthCheck> PerformSecurityHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var apiKeyStats = await _apiKeyManager.GetUsageStatisticsAsync(cancellationToken);
            var recentEvents = await _auditLogger.GetSecurityEventsAsync(
                DateTimeOffset.UtcNow.AddDays(-7), 
                DateTimeOffset.UtcNow, 
                cancellationToken);

            var healthCheck = new SecurityHealthCheck
            {
                Timestamp = DateTimeOffset.UtcNow,
                OverallScore = CalculateOverallSecurityScore(apiKeyStats, recentEvents),
                ApiKeyHealth = CalculateApiKeyHealth(apiKeyStats),
                SecurityEventHealth = CalculateSecurityEventHealth(recentEvents),
                Recommendations = await GenerateHealthCheckRecommendationsAsync(apiKeyStats, recentEvents, cancellationToken)
            };

            return healthCheck;
        }
    }
}
