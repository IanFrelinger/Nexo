using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Provides comprehensive audit logging and security monitoring capabilities
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs a security event
    /// </summary>
    /// <param name="auditEvent">Audit event to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logging result</returns>
    Task<AuditLogResult> LogSecurityEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs user authentication events
    /// </summary>
    /// <param name="authEvent">Authentication event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logging result</returns>
    Task<AuditLogResult> LogAuthenticationEventAsync(AuthenticationAuditEvent authEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs authorization events
    /// </summary>
    /// <param name="authEvent">Authorization event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logging result</returns>
    Task<AuditLogResult> LogAuthorizationEventAsync(AuthorizationAuditEvent authEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs data access events
    /// </summary>
    /// <param name="dataEvent">Data access event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logging result</returns>
    Task<AuditLogResult> LogDataAccessEventAsync(DataAccessAuditEvent dataEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs system configuration changes
    /// </summary>
    /// <param name="configEvent">Configuration change event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logging result</returns>
    Task<AuditLogResult> LogConfigurationChangeEventAsync(ConfigurationChangeAuditEvent configEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs encryption/decryption events
    /// </summary>
    /// <param name="cryptoEvent">Cryptography event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logging result</returns>
    Task<AuditLogResult> LogCryptographyEventAsync(CryptographyAuditEvent cryptoEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs with filtering
    /// </summary>
    /// <param name="filter">Audit log filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered audit logs</returns>
    Task<AuditLogQueryResult> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs for compliance reporting
    /// </summary>
    /// <param name="exportRequest">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<AuditLogExportResult> ExportAuditLogsAsync(AuditLogExportRequest exportRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit log statistics
    /// </summary>
    /// <param name="statsRequest">Statistics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit log statistics</returns>
    Task<AuditLogStatistics> GetAuditLogStatisticsAsync(AuditLogStatisticsRequest statsRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old audit logs
    /// </summary>
    /// <param name="archiveRequest">Archive request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Archive result</returns>
    Task<AuditLogArchiveResult> ArchiveAuditLogsAsync(AuditLogArchiveRequest archiveRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit log configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit log configuration</returns>
    Task<AuditLogConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base audit event
/// </summary>
public record AuditEvent
{
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string SourceIp { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Authentication audit event
/// </summary>
public record AuthenticationAuditEvent : AuditEvent
{
    public string AuthenticationMethod { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public bool IsMfaRequired { get; init; }
    public bool IsMfaCompleted { get; init; }
    public string? SessionId { get; init; }
    public string? TokenId { get; init; }
    public DateTime? TokenExpiresAt { get; init; }
    public List<string> UserRoles { get; init; } = new();
    public List<string> UserPermissions { get; init; } = new();
}

/// <summary>
/// Authorization audit event
/// </summary>
public record AuthorizationAuditEvent : AuditEvent
{
    public List<string> RequiredRoles { get; init; } = new();
    public List<string> RequiredPermissions { get; init; } = new();
    public List<string> UserRoles { get; init; } = new();
    public List<string> UserPermissions { get; init; } = new();
    public string Decision { get; init; } = string.Empty;
    public string? DenialReason { get; init; }
    public Dictionary<string, object> PolicyContext { get; init; } = new();
}

/// <summary>
/// Data access audit event
/// </summary>
public record DataAccessAuditEvent : AuditEvent
{
    public string DataType { get; init; } = string.Empty;
    public string DataId { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public bool IsSensitiveData { get; init; }
    public string? DataHash { get; init; }
    public Dictionary<string, object> DataContext { get; init; } = new();
}

/// <summary>
/// Configuration change audit event
/// </summary>
public record ConfigurationChangeAuditEvent : AuditEvent
{
    public string ConfigurationKey { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string ChangeType { get; init; } = string.Empty;
    public string? ApprovalId { get; init; }
    public Dictionary<string, object> ConfigurationContext { get; init; } = new();
}

/// <summary>
/// Cryptography audit event
/// </summary>
public record CryptographyAuditEvent : AuditEvent
{
    public string Algorithm { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string? DataHash { get; init; }
    public bool IsKeyRotation { get; init; }
    public Dictionary<string, object> CryptoContext { get; init; } = new();
}

/// <summary>
/// Audit log result
/// </summary>
public record AuditLogResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string LogId { get; init; } = string.Empty;
    public DateTime LoggedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Audit log filter
/// </summary>
public record AuditLogFilter
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public string? EventType { get; init; }
    public string? Severity { get; init; }
    public string? Resource { get; init; }
    public string? Action { get; init; }
    public bool? IsSuccessful { get; init; }
    public string? SourceIp { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 100;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Audit log query result
/// </summary>
public record AuditLogQueryResult
{
    public List<AuditEvent> Events { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public DateTime RetrievedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Audit log export request
/// </summary>
public record AuditLogExportRequest
{
    public AuditLogFilter Filter { get; init; } = new();
    public string Format { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public bool IncludeMetadata { get; init; }
    public List<string> CustomFields { get; init; } = new();
}

/// <summary>
/// Audit log export result
/// </summary>
public record AuditLogExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int ExportedCount { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Audit log statistics request
/// </summary>
public record AuditLogStatisticsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? GroupBy { get; init; }
    public List<string> Metrics { get; init; } = new();
    public string? UserId { get; init; }
    public string? EventType { get; init; }
}

/// <summary>
/// Audit log statistics
/// </summary>
public record AuditLogStatistics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalEvents { get; init; }
    public int SuccessfulEvents { get; init; }
    public int FailedEvents { get; init; }
    public Dictionary<string, int> EventsByType { get; init; } = new();
    public Dictionary<string, int> EventsBySeverity { get; init; } = new();
    public Dictionary<string, int> EventsByUser { get; init; } = new();
    public Dictionary<string, int> EventsByResource { get; init; } = new();
    public List<TimeSeriesData> TimeSeriesData { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Time series data
/// </summary>
public record TimeSeriesData
{
    public DateTime Timestamp { get; init; }
    public int Count { get; init; }
    public Dictionary<string, int> Breakdown { get; init; } = new();
}

/// <summary>
/// Audit log archive request
/// </summary>
public record AuditLogArchiveRequest
{
    public DateTime ArchiveBefore { get; init; }
    public string ArchiveLocation { get; init; } = string.Empty;
    public string ArchiveFormat { get; init; } = string.Empty;
    public bool CompressArchive { get; init; }
    public bool DeleteAfterArchive { get; init; }
    public string? RetentionPolicy { get; init; }
}

/// <summary>
/// Audit log archive result
/// </summary>
public record AuditLogArchiveResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ArchivePath { get; init; } = string.Empty;
    public long ArchiveSize { get; init; }
    public int ArchivedCount { get; init; }
    public int DeletedCount { get; init; }
    public DateTime ArchivedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Audit log configuration
/// </summary>
public record AuditLogConfiguration
{
    public bool EnableAuditLogging { get; init; }
    public List<string> EnabledEventTypes { get; init; } = new();
    public List<string> ExcludedEventTypes { get; init; } = new();
    public List<string> SensitiveFields { get; init; } = new();
    public bool LogSensitiveData { get; init; }
    public bool EncryptAuditLogs { get; init; }
    public string EncryptionKey { get; init; } = string.Empty;
    public TimeSpan RetentionPeriod { get; init; }
    public string StorageLocation { get; init; } = string.Empty;
    public bool EnableRealTimeAlerts { get; init; }
    public List<string> AlertConditions { get; init; } = new();
    public Dictionary<string, object> CustomSettings { get; init; } = new();
} 