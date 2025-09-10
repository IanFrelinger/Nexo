using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Security
{
    /// <summary>
    /// Interface for audit logging of security events and operations.
    /// Part of Phase 3.3 security and compliance features.
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        /// Logs an audit event.
        /// </summary>
        /// <param name="auditEvent">The audit event to log.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs a security event.
        /// </summary>
        /// <param name="securityEvent">The security event to log.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs a compliance event.
        /// </summary>
        /// <param name="complianceEvent">The compliance event to log.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogComplianceEventAsync(ComplianceEvent complianceEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets audit events for a specific time range.
        /// </summary>
        /// <param name="startTime">Start time for the query.</param>
        /// <param name="endTime">End time for the query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of audit events.</returns>
        Task<IEnumerable<AuditEvent>> GetAuditEventsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets security events for a specific time range.
        /// </summary>
        /// <param name="startTime">Start time for the query.</param>
        /// <param name="endTime">End time for the query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of security events.</returns>
        Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets compliance events for a specific time range.
        /// </summary>
        /// <param name="startTime">Start time for the query.</param>
        /// <param name="endTime">End time for the query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of compliance events.</returns>
        Task<IEnumerable<ComplianceEvent>> GetComplianceEventsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an audit report for a specific time range.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Audit report.</returns>
        Task<AuditReport> GenerateAuditReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base audit event model.
    /// </summary>
    public abstract class BaseAuditEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// General audit event.
    /// </summary>
    public class AuditEvent : BaseAuditEvent
    {
        public AuditEventType EventType { get; set; }
        public string Description { get; set; } = string.Empty;
        public AuditEventSeverity Severity { get; set; } = AuditEventSeverity.Info;
    }

    /// <summary>
    /// Security-specific audit event.
    /// </summary>
    public class SecurityEvent : BaseAuditEvent
    {
        public SecurityEventType EventType { get; set; }
        public string Description { get; set; } = string.Empty;
        public SecurityEventSeverity Severity { get; set; } = SecurityEventSeverity.Info;
        public string? ThreatLevel { get; set; }
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// Compliance-specific audit event.
    /// </summary>
    public class ComplianceEvent : BaseAuditEvent
    {
        public ComplianceEventType EventType { get; set; }
        public string Description { get; set; } = string.Empty;
        public ComplianceEventSeverity Severity { get; set; } = ComplianceEventSeverity.Info;
        public string? Regulation { get; set; }
        public string? Requirement { get; set; }
    }

    /// <summary>
    /// Audit event types.
    /// </summary>
    public enum AuditEventType
    {
        UserLogin,
        UserLogout,
        UserRegistration,
        UserProfileUpdate,
        PasswordChange,
        PermissionChange,
        DataAccess,
        DataModification,
        DataDeletion,
        SystemConfiguration,
        SystemStartup,
        SystemShutdown,
        Error,
        Warning
    }

    /// <summary>
    /// Security event types.
    /// </summary>
    public enum SecurityEventType
    {
        AuthenticationFailure,
        AuthorizationFailure,
        SuspiciousActivity,
        BruteForceAttempt,
        UnauthorizedAccess,
        DataBreach,
        MalwareDetection,
        PhishingAttempt,
        DdosAttack,
        SqlInjection,
        XssAttack,
        CsrfAttack,
        SessionHijacking,
        PrivilegeEscalation
    }

    /// <summary>
    /// Compliance event types.
    /// </summary>
    public enum ComplianceEventType
    {
        DataRetention,
        DataDeletion,
        DataExport,
        DataImport,
        ConsentGiven,
        ConsentWithdrawn,
        PrivacyPolicyUpdate,
        TermsOfServiceUpdate,
        DataProcessing,
        DataSharing,
        DataAnonymization,
        AuditTrailAccess,
        ComplianceViolation,
        RemediationAction
    }

    /// <summary>
    /// Audit event severity levels.
    /// </summary>
    public enum AuditEventSeverity
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Security event severity levels.
    /// </summary>
    public enum SecurityEventSeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Compliance event severity levels.
    /// </summary>
    public enum ComplianceEventSeverity
    {
        Info,
        Warning,
        Violation,
        Critical
    }

    /// <summary>
    /// Audit report model.
    /// </summary>
    public class AuditReport
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int TotalEvents { get; set; }
        public int AuditEvents { get; set; }
        public int SecurityEvents { get; set; }
        public int ComplianceEvents { get; set; }
        public Dictionary<AuditEventType, int> EventsByType { get; set; } = new Dictionary<AuditEventType, int>();
        public Dictionary<AuditEventSeverity, int> EventsBySeverity { get; set; } = new Dictionary<AuditEventSeverity, int>();
        public List<string> TopUsers { get; set; } = new List<string>();
        public List<string> TopResources { get; set; } = new List<string>();
        public List<AuditEvent> RecentEvents { get; set; } = new List<AuditEvent>();
    }
}