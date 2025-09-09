using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Security
{
    /// <summary>
    /// Interface for production-ready security auditing and penetration testing.
    /// Part of Phase 3.4 production readiness features.
    /// </summary>
    public interface IProductionSecurityAuditor
    {
        /// <summary>
        /// Runs comprehensive security audit across all systems.
        /// </summary>
        /// <param name="options">Security audit options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Security audit result.</returns>
        Task<SecurityAuditResult> RunSecurityAuditAsync(
            SecurityAuditOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs penetration testing simulation.
        /// </summary>
        /// <param name="options">Penetration test options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Penetration test result.</returns>
        Task<PenetrationTestResult> RunPenetrationTestAsync(
            PenetrationTestOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets security recommendations based on audit results.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of security recommendations.</returns>
        Task<IEnumerable<SecurityRecommendation>> GetSecurityRecommendationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets security compliance status.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Security compliance status.</returns>
        Task<SecurityComplianceStatus> GetSecurityComplianceStatusAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Security audit options.
    /// </summary>
    public class SecurityAuditOptions
    {
        public bool AuditApiKeys { get; set; } = true;
        public bool AuditAuthentication { get; set; } = true;
        public bool AuditAuthorization { get; set; } = true;
        public bool AuditEncryption { get; set; } = true;
        public bool AuditAuditLogging { get; set; } = true;
        public bool AuditNetwork { get; set; } = true;
        public bool AuditCompliance { get; set; } = true;
        public bool AuditPerformance { get; set; } = true;
        public TimeSpan MaxAuditTime { get; set; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Penetration test options.
    /// </summary>
    public class PenetrationTestOptions
    {
        public string TestName { get; set; } = "Default";
        public bool TestAuthenticationBypass { get; set; } = true;
        public bool TestAuthorizationEscalation { get; set; } = true;
        public bool TestApiKeySecurity { get; set; } = true;
        public bool TestDataInjection { get; set; } = true;
        public bool TestSessionManagement { get; set; } = true;
        public bool TestInputValidation { get; set; } = true;
        public TimeSpan MaxTestTime { get; set; } = TimeSpan.FromMinutes(20);
    }

    /// <summary>
    /// Security audit result.
    /// </summary>
    public class SecurityAuditResult
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public SecurityAuditOptions Options { get; set; } = new();
        public double OverallSecurityScore { get; set; }
        
        public ApiKeySecurityAudit? ApiKeyAudit { get; set; }
        public AuthenticationSecurityAudit? AuthenticationAudit { get; set; }
        public AuthorizationSecurityAudit? AuthorizationAudit { get; set; }
        public DataEncryptionAudit? EncryptionAudit { get; set; }
        public AuditLoggingSecurityAudit? AuditLoggingAudit { get; set; }
        public NetworkSecurityAudit? NetworkAudit { get; set; }
        public ComplianceAudit? ComplianceAudit { get; set; }
        public PerformanceSecurityAudit? PerformanceSecurityAudit { get; set; }
    }

    /// <summary>
    /// Penetration test result.
    /// </summary>
    public class PenetrationTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public PenetrationTestOptions Options { get; set; } = new();
        public int VulnerabilityCount { get; set; }
        public SecurityRating SecurityRating { get; set; }
        
        public AuthenticationBypassTestResult? AuthenticationBypassResults { get; set; }
        public AuthorizationEscalationTestResult? AuthorizationEscalationResults { get; set; }
        public ApiKeySecurityTestResult? ApiKeySecurityResults { get; set; }
        public DataInjectionTestResult? DataInjectionResults { get; set; }
        public SessionManagementTestResult? SessionManagementResults { get; set; }
        public InputValidationTestResult? InputValidationResults { get; set; }
    }

    /// <summary>
    /// Security recommendation.
    /// </summary>
    public class SecurityRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public SecurityPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EstimatedImpact { get; set; } = string.Empty;
        public string ImplementationEffort { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security compliance status.
    /// </summary>
    public class SecurityComplianceStatus
    {
        public DateTimeOffset CheckTime { get; set; }
        public double OverallComplianceScore { get; set; }
        public bool IsCompliant { get; set; }
        public string? ErrorMessage { get; set; }
        
        public ComplianceCheckResult? GDPRCompliance { get; set; }
        public ComplianceCheckResult? HIPAACompliance { get; set; }
        public ComplianceCheckResult? SOXCompliance { get; set; }
        public ComplianceCheckResult? ISO27001Compliance { get; set; }
        public ComplianceCheckResult? PCICompliance { get; set; }
    }

    /// <summary>
    /// Security priority level.
    /// </summary>
    public enum SecurityPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Security rating.
    /// </summary>
    public enum SecurityRating
    {
        Poor,
        Fair,
        Good,
        Excellent
    }

    #region Security Audit Types

    /// <summary>
    /// API key security audit.
    /// </summary>
    public class ApiKeySecurityAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public int WeakKeyCount { get; set; }
        public int ExpiredKeyCount { get; set; }
        public int OldKeyCount { get; set; }
    }

    /// <summary>
    /// Authentication security audit.
    /// </summary>
    public class AuthenticationSecurityAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool MultiFactorAuthenticationEnabled { get; set; }
        public double PasswordPolicyStrength { get; set; }
        public int SessionTimeoutMinutes { get; set; }
        public bool AccountLockoutEnabled { get; set; }
        public bool BruteForceProtectionEnabled { get; set; }
    }

    /// <summary>
    /// Authorization security audit.
    /// </summary>
    public class AuthorizationSecurityAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool RoleBasedAccessControlEnabled { get; set; }
        public bool PermissionBasedAccessControlEnabled { get; set; }
        public bool LeastPrivilegePrincipleImplemented { get; set; }
        public int AccessReviewFrequencyDays { get; set; }
    }

    /// <summary>
    /// Data encryption audit.
    /// </summary>
    public class DataEncryptionAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool DataAtRestEncrypted { get; set; }
        public bool DataInTransitEncrypted { get; set; }
        public double EncryptionAlgorithmStrength { get; set; }
        public bool KeyManagementSecure { get; set; }
    }

    /// <summary>
    /// Audit logging security audit.
    /// </summary>
    public class AuditLoggingSecurityAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool ComprehensiveLoggingEnabled { get; set; }
        public bool LogIntegrityProtected { get; set; }
        public bool LogRetentionPolicyCompliant { get; set; }
        public bool RealTimeMonitoringEnabled { get; set; }
    }

    /// <summary>
    /// Network security audit.
    /// </summary>
    public class NetworkSecurityAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool FirewallEnabled { get; set; }
        public bool IntrusionDetectionEnabled { get; set; }
        public bool SSLTLSEnabled { get; set; }
        public bool NetworkSegmentationImplemented { get; set; }
    }

    /// <summary>
    /// Compliance audit.
    /// </summary>
    public class ComplianceAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool GDPRCompliant { get; set; }
        public bool HIPAACompliant { get; set; }
        public bool SOXCompliant { get; set; }
        public bool ISO27001Compliant { get; set; }
    }

    /// <summary>
    /// Performance security audit.
    /// </summary>
    public class PerformanceSecurityAudit
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
        public bool ResponseTimeSecure { get; set; }
        public bool ResourceUsageSecure { get; set; }
        public bool CacheSecurityOptimal { get; set; }
    }

    #endregion

    #region Penetration Test Results

    /// <summary>
    /// Authentication bypass test result.
    /// </summary>
    public class AuthenticationBypassTestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int SQLInjectionAttempts { get; set; }
        public int NoSQLInjectionAttempts { get; set; }
        public int AuthenticationBypassAttempts { get; set; }
        public int SessionFixationAttempts { get; set; }
    }

    /// <summary>
    /// Authorization escalation test result.
    /// </summary>
    public class AuthorizationEscalationTestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int PrivilegeEscalationAttempts { get; set; }
        public int HorizontalEscalationAttempts { get; set; }
        public int VerticalEscalationAttempts { get; set; }
    }

    /// <summary>
    /// API key security test result.
    /// </summary>
    public class ApiKeySecurityTestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int BruteForceAttempts { get; set; }
        public int KeyEnumerationAttempts { get; set; }
        public int KeyLeakageAttempts { get; set; }
    }

    /// <summary>
    /// Data injection test result.
    /// </summary>
    public class DataInjectionTestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int SQLInjectionAttempts { get; set; }
        public int NoSQLInjectionAttempts { get; set; }
        public int XSSAttempts { get; set; }
        public int CommandInjectionAttempts { get; set; }
    }

    /// <summary>
    /// Session management test result.
    /// </summary>
    public class SessionManagementTestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int SessionHijackingAttempts { get; set; }
        public int SessionFixationAttempts { get; set; }
        public int SessionTimeoutBypassAttempts { get; set; }
    }

    /// <summary>
    /// Input validation test result.
    /// </summary>
    public class InputValidationTestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int MaliciousInputAttempts { get; set; }
        public int BufferOverflowAttempts { get; set; }
        public int FormatStringAttempts { get; set; }
    }

    #endregion

    #region Compliance Types

    /// <summary>
    /// Compliance check result.
    /// </summary>
    public class ComplianceCheckResult
    {
        public string Standard { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public double Score { get; set; }
        public DateTimeOffset LastChecked { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    #endregion
}
