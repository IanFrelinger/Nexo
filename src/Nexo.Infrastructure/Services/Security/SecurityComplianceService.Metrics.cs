using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Metrics calculation functionality for security compliance service.
    /// </summary>
    public partial class SecurityComplianceService
    {
        /// <summary>
        /// Calculates security metrics from audit data.
        /// </summary>
        private SecurityMetrics CalculateSecurityMetrics(AuditReport auditReport, ApiKeyUsageStatistics apiKeyStats)
        {
            return new SecurityMetrics
            {
                TotalApiKeys = apiKeyStats.TotalKeys,
                ActiveApiKeys = apiKeyStats.ActiveKeys,
                ExpiredApiKeys = apiKeyStats.ExpiredKeys,
                RevokedApiKeys = apiKeyStats.RevokedKeys,
                FailedAuthenticationAttempts = auditReport.SecurityEvents,
                SuccessfulAuthenticationAttempts = auditReport.AuditEvents,
                SecurityEventRate = CalculateEventRate(auditReport.SecurityEvents, auditReport.StartTime, auditReport.EndTime),
                AverageResponseTime = TimeSpan.Zero, // Would be calculated from actual metrics
                ThreatLevel = DetermineThreatLevel(auditReport.SecurityEvents)
            };
        }

        /// <summary>
        /// Calculates compliance metrics from audit data.
        /// </summary>
        private ComplianceMetrics CalculateComplianceMetrics(AuditReport auditReport)
        {
            return new ComplianceMetrics
            {
                TotalComplianceEvents = auditReport.ComplianceEvents,
                DataRetentionEvents = 0, // Would be calculated from actual compliance events
                DataDeletionEvents = 0,
                ConsentEvents = 0,
                PrivacyPolicyEvents = 0,
                ComplianceViolations = 0,
                ComplianceScore = CalculateComplianceScore(auditReport.ComplianceEvents)
            };
        }

        /// <summary>
        /// Calculates event rate per hour.
        /// </summary>
        private double CalculateEventRate(int eventCount, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            var hours = (endTime - startTime).TotalHours;
            return hours > 0 ? eventCount / hours : 0;
        }

        /// <summary>
        /// Determines threat level based on security events.
        /// </summary>
        private string DetermineThreatLevel(int securityEventCount)
        {
            return securityEventCount switch
            {
                < 5 => "Low",
                < 20 => "Medium",
                < 50 => "High",
                _ => "Critical"
            };
        }

        /// <summary>
        /// Calculates compliance score.
        /// </summary>
        private int CalculateComplianceScore(int complianceEvents)
        {
            // Simple scoring based on compliance events
            return complianceEvents switch
            {
                0 => 100,
                < 5 => 90,
                < 10 => 80,
                < 20 => 70,
                _ => 60
            };
        }
    }
}
