using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Recommendations functionality for security compliance service.
    /// </summary>
    public partial class SecurityComplianceService
    {
        /// <summary>
        /// Generates security recommendations based on audit data.
        /// </summary>
        private Task<List<SecurityRecommendation>> GenerateSecurityRecommendationsAsync(
            AuditReport auditReport, 
            ApiKeyUsageStatistics apiKeyStats, 
            CancellationToken cancellationToken)
        {
            var recommendations = new List<SecurityRecommendation>();

            // API Key recommendations
            if (apiKeyStats.ExpiredKeys > 0)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Category = "ApiKeyManagement",
                    Priority = SecurityPriority.Medium,
                    Title = "Clean up expired API keys",
                    Description = $"There are {apiKeyStats.ExpiredKeys} expired API keys that should be removed.",
                    EstimatedImpact = "Medium",
                    ImplementationEffort = "Low"
                });
            }

            if (apiKeyStats.RevokedKeys > apiKeyStats.ActiveKeys * 0.1)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Category = "ApiKeyManagement",
                    Priority = SecurityPriority.High,
                    Title = "High API key revocation rate",
                    Description = "A high number of API keys have been revoked recently. Review security practices.",
                    EstimatedImpact = "High",
                    ImplementationEffort = "Medium"
                });
            }

            // Security event recommendations
            if (auditReport.SecurityEvents > auditReport.AuditEvents * 0.1)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Category = "SecurityMonitoring",
                    Priority = SecurityPriority.High,
                    Title = "High security event rate",
                    Description = "A high number of security events detected. Review security posture.",
                    EstimatedImpact = "High",
                    ImplementationEffort = "High"
                });
            }

            return Task.FromResult(recommendations);
        }

        /// <summary>
        /// Identifies compliance violations from audit data.
        /// </summary>
        private Task<List<ComplianceViolation>> IdentifyComplianceViolationsAsync(
            AuditReport auditReport, 
            IEnumerable<ApiKeyInfo> apiKeys, 
            CancellationToken cancellationToken)
        {
            var violations = new List<ComplianceViolation>();

            // Check for long-lived API keys without expiration
            var longLivedKeys = apiKeys.Where(k => k.IsActive && !k.ExpiresAt.HasValue && 
                k.CreatedAt < DateTimeOffset.UtcNow.AddDays(-90)).ToList();

            foreach (var key in longLivedKeys)
            {
                violations.Add(new ComplianceViolation
                {
                    Type = ComplianceViolationType.LongLivedApiKey,
                    Severity = ComplianceViolationSeverity.Medium,
                    Description = $"API key '{key.Name}' has been active for over 90 days without expiration",
                    Resource = key.Id,
                    DetectedAt = DateTimeOffset.UtcNow,
                    Remediation = "Set an expiration date for the API key or rotate it regularly"
                });
            }

            return Task.FromResult(violations);
        }
    }
}
