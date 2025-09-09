using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Security
{
    /// <summary>
    /// Interface for security compliance service that integrates API key management,
    /// audit logging, and compliance reporting for Phase 3.3.
    /// </summary>
    public interface ISecurityComplianceService
    {
        /// <summary>
        /// Validates API key and logs the access attempt.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <param name="requiredPermission">Optional required permission.</param>
        /// <param name="userId">User ID for audit logging.</param>
        /// <param name="ipAddress">IP address for audit logging.</param>
        /// <param name="userAgent">User agent for audit logging.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with audit trail.</returns>
        Task<ApiKeyValidationResult> ValidateApiKeyWithAuditAsync(
            string apiKey, 
            string? requiredPermission = null,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a new API key with audit logging.
        /// </summary>
        /// <param name="name">Name for the API key.</param>
        /// <param name="description">Description of the API key.</param>
        /// <param name="userId">User ID for audit logging.</param>
        /// <param name="expiration">Optional expiration time.</param>
        /// <param name="permissions">Optional permissions for the key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated API key information.</returns>
        Task<ApiKeyInfo> GenerateApiKeyWithAuditAsync(
            string name, 
            string description, 
            string userId,
            TimeSpan? expiration = null,
            IEnumerable<string>? permissions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes an API key with audit logging.
        /// </summary>
        /// <param name="keyId">ID of the key to revoke.</param>
        /// <param name="userId">User ID for audit logging.</param>
        /// <param name="reason">Reason for revocation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key was revoked successfully.</returns>
        Task<bool> RevokeApiKeyWithAuditAsync(
            string keyId, 
            string userId,
            string? reason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a comprehensive security compliance report.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Security compliance report.</returns>
        Task<SecurityComplianceReport> GenerateComplianceReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a security health check.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Security health check results.</returns>
        Task<SecurityHealthCheck> PerformSecurityHealthCheckAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Security compliance report model.
    /// </summary>
    public class SecurityComplianceReport
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int TotalEvents { get; set; }
        public int SecurityEvents { get; set; }
        public int ComplianceEvents { get; set; }
        public ApiKeyUsageStatistics ApiKeyStatistics { get; set; } = new ApiKeyUsageStatistics();
        public SecurityMetrics SecurityMetrics { get; set; } = new SecurityMetrics();
        public ComplianceMetrics ComplianceMetrics { get; set; } = new ComplianceMetrics();
        public List<SecurityRecommendation> Recommendations { get; set; } = new List<SecurityRecommendation>();
        public List<ComplianceViolation> Violations { get; set; } = new List<ComplianceViolation>();
    }

    /// <summary>
    /// Security metrics model.
    /// </summary>
    public class SecurityMetrics
    {
        public int TotalApiKeys { get; set; }
        public int ActiveApiKeys { get; set; }
        public int ExpiredApiKeys { get; set; }
        public int RevokedApiKeys { get; set; }
        public int FailedAuthenticationAttempts { get; set; }
        public int SuccessfulAuthenticationAttempts { get; set; }
        public double SecurityEventRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public string ThreatLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Compliance metrics model.
    /// </summary>
    public class ComplianceMetrics
    {
        public int TotalComplianceEvents { get; set; }
        public int DataRetentionEvents { get; set; }
        public int DataDeletionEvents { get; set; }
        public int ConsentEvents { get; set; }
        public int PrivacyPolicyEvents { get; set; }
        public int ComplianceViolations { get; set; }
        public int ComplianceScore { get; set; }
    }

    /// <summary>
    /// Security recommendation model.
    /// </summary>
    public class SecurityRecommendation
    {
        public SecurityRecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security recommendation types.
    /// </summary>
    public enum SecurityRecommendationType
    {
        ApiKeyManagement,
        SecurityMonitoring,
        AccessControl,
        DataProtection,
        Compliance
    }

    /// <summary>
    /// Security health check model.
    /// </summary>
    public class SecurityHealthCheck
    {
        public DateTimeOffset Timestamp { get; set; }
        public int OverallScore { get; set; }
        public int ApiKeyHealth { get; set; }
        public int SecurityEventHealth { get; set; }
        public List<SecurityRecommendation> Recommendations { get; set; } = new List<SecurityRecommendation>();
    }

    /// <summary>
    /// Compliance violation model.
    /// </summary>
    public class ComplianceViolation
    {
        public ComplianceViolationType Type { get; set; }
        public ComplianceViolationSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public DateTimeOffset DetectedAt { get; set; }
        public string Remediation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Compliance violation types.
    /// </summary>
    public enum ComplianceViolationType
    {
        LongLivedApiKey,
        MissingExpiration,
        ExcessivePermissions,
        DataRetentionViolation,
        AccessControlViolation,
        AuditTrailGap
    }

    /// <summary>
    /// Compliance violation severity levels.
    /// </summary>
    public enum ComplianceViolationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}