using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Security compliance service that integrates API key management, audit logging,
    /// and compliance reporting for Phase 3.3.
    /// </summary>
    public class SecurityComplianceService : ISecurityComplianceService
    {
        private readonly ISecureApiKeyManager _apiKeyManager;
        private readonly IAuditLogger _auditLogger;

        public SecurityComplianceService(ISecureApiKeyManager apiKeyManager, IAuditLogger auditLogger)
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        }

        /// <summary>
        /// Validates API key and logs the access attempt.
        /// </summary>
        public async Task<ApiKeyValidationResult> ValidateApiKeyWithAuditAsync(
            string apiKey, 
            string? requiredPermission = null,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _apiKeyManager.ValidateApiKeyAsync(apiKey, requiredPermission, cancellationToken);

            // Log the access attempt
            var auditEvent = new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationFailure,
                Description = validationResult.IsValid ? "API key validation successful" : $"API key validation failed: {validationResult.ErrorMessage}",
                Severity = validationResult.IsValid ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium,
                UserId = userId ?? "unknown",
                Resource = "API",
                Action = "ValidateApiKey",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsBlocked = !validationResult.IsValid
            };

            await _auditLogger.LogSecurityEventAsync(auditEvent, cancellationToken);

            return validationResult;
        }

        /// <summary>
        /// Generates a new API key with audit logging.
        /// </summary>
        public async Task<ApiKeyInfo> GenerateApiKeyWithAuditAsync(
            string name, 
            string description, 
            string userId,
            TimeSpan? expiration = null,
            IEnumerable<string>? permissions = null,
            CancellationToken cancellationToken = default)
        {
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(name, description, expiration, permissions, cancellationToken);

            // Log the API key generation
            var auditEvent = new AuditEvent
            {
                EventType = AuditEventType.SystemConfiguration,
                Description = $"API key generated: {name}",
                Severity = AuditEventSeverity.Info,
                UserId = userId,
                Resource = "API Key Management",
                Action = "GenerateApiKey",
                Metadata = new Dictionary<string, object>
                {
                    ["KeyId"] = apiKey.Id,
                    ["KeyName"] = apiKey.Name,
                    ["Expiration"] = apiKey.ExpiresAt?.ToString() ?? "Never",
                    ["Permissions"] = string.Join(",", apiKey.Permissions)
                }
            };

            await _auditLogger.LogAuditEventAsync(auditEvent, cancellationToken);

            return apiKey;
        }

        /// <summary>
        /// Revokes an API key with audit logging.
        /// </summary>
        public async Task<bool> RevokeApiKeyWithAuditAsync(
            string keyId, 
            string userId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var success = await _apiKeyManager.RevokeApiKeyAsync(keyId, cancellationToken);

            if (success)
            {
                // Log the API key revocation
                var auditEvent = new AuditEvent
                {
                    EventType = AuditEventType.SystemConfiguration,
                    Description = $"API key revoked: {keyId}",
                    Severity = AuditEventSeverity.Warning,
                    UserId = userId,
                    Resource = "API Key Management",
                    Action = "RevokeApiKey",
                    Metadata = new Dictionary<string, object>
                    {
                        ["KeyId"] = keyId,
                        ["Reason"] = reason ?? "No reason provided"
                    }
                };

                await _auditLogger.LogAuditEventAsync(auditEvent, cancellationToken);
            }

            return success;
        }

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
        /// Generates security recommendations based on audit data.
        /// </summary>
        private async Task<List<SecurityRecommendation>> GenerateSecurityRecommendationsAsync(
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

            return recommendations;
        }

        /// <summary>
        /// Identifies compliance violations from audit data.
        /// </summary>
        private async Task<List<ComplianceViolation>> IdentifyComplianceViolationsAsync(
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

            return violations;
        }

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
        private async Task<List<SecurityRecommendation>> GenerateHealthCheckRecommendationsAsync(
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

            return recommendations;
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