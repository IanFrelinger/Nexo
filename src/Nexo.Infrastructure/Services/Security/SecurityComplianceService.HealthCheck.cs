using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Health check functionality for security compliance service.
    /// </summary>
    public partial class SecurityComplianceService
    {
        /// <summary>
        /// Calculates overall security score.
        /// </summary>
        private int CalculateOverallSecurityScore(ApiKeyUsageStatistics apiKeyStats, IEnumerable<SecurityEvent> recentEvents)
        {
            var score = 100;

            // Deduct points for expired keys
            if (apiKeyStats.ExpiredKeys > 0)
                score -= Math.Min(apiKeyStats.ExpiredKeys * 5, 20);

            // Deduct points for revoked keys
            if (apiKeyStats.RevokedKeys > apiKeyStats.ActiveKeys * 0.1)
                score -= 15;

            // Deduct points for security events
            var securityEventCount = recentEvents.Count();
            if (securityEventCount > 10)
                score -= Math.Min(securityEventCount * 2, 30);

            return Math.Max(score, 0);
        }

        /// <summary>
        /// Calculates API key health score.
        /// </summary>
        private int CalculateApiKeyHealth(ApiKeyUsageStatistics apiKeyStats)
        {
            if (apiKeyStats.TotalKeys == 0) return 100;

            var expiredRatio = (double)apiKeyStats.ExpiredKeys / apiKeyStats.TotalKeys;
            var revokedRatio = (double)apiKeyStats.RevokedKeys / apiKeyStats.TotalKeys;

            var score = 100;
            score -= (int)(expiredRatio * 30);
            score -= (int)(revokedRatio * 20);

            return Math.Max(score, 0);
        }

        /// <summary>
        /// Calculates security event health score.
        /// </summary>
        private int CalculateSecurityEventHealth(IEnumerable<SecurityEvent> recentEvents)
        {
            var events = recentEvents.ToList();
            if (!events.Any()) return 100;

            var criticalEvents = events.Count(e => e.Severity == SecurityEventSeverity.Critical);
            var highEvents = events.Count(e => e.Severity == SecurityEventSeverity.High);

            var score = 100;
            score -= criticalEvents * 20;
            score -= highEvents * 10;

            return Math.Max(score, 0);
        }

        /// <summary>
        /// Generates health check recommendations.
        /// </summary>
        private Task<List<SecurityRecommendation>> GenerateHealthCheckRecommendationsAsync(
            ApiKeyUsageStatistics apiKeyStats, 
            IEnumerable<SecurityEvent> recentEvents, 
            CancellationToken cancellationToken)
        {
            var recommendations = new List<SecurityRecommendation>();

            if (apiKeyStats.ExpiredKeys > 0)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Category = "ApiKeyManagement",
                    Priority = SecurityPriority.Medium,
                    Title = "Clean up expired API keys",
                    Description = $"Remove {apiKeyStats.ExpiredKeys} expired API keys",
                    EstimatedImpact = "Medium",
                    ImplementationEffort = "Low"
                });
            }

            var criticalEvents = recentEvents.Count(e => e.Severity == SecurityEventSeverity.Critical);
            if (criticalEvents > 0)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Category = "SecurityMonitoring",
                    Priority = SecurityPriority.Critical,
                    Title = "Address critical security events",
                    Description = $"{criticalEvents} critical security events require immediate attention",
                    EstimatedImpact = "Critical",
                    ImplementationEffort = "High"
                });
            }

            return Task.FromResult(recommendations);
        }
    }
}
