using Nexo.Core.Application.Trust.Models;

namespace Nexo.Core.Application.Trust.Ports;

/// <summary>
/// Unified audit log for data decisions: sanitization, boundary changes, classification.
/// Export for compliance (structured JSON).
/// </summary>
public interface IDataDecisionAuditLog
{
    /// <summary>Log a sanitization event.</summary>
    void LogSanitization(SanitizationAuditEntryDto entry);

    /// <summary>Log a boundary change event.</summary>
    void LogBoundaryChange(BoundaryChangeEvent evt);

    /// <summary>Log a classification event.</summary>
    void LogClassification(string dataType, string levelName, string? reason);

    /// <summary>Get recent audit entries (all types).</summary>
    IReadOnlyList<DataDecisionAuditEntry> GetRecent(int maxCount, DateTimeOffset? since = null);

    /// <summary>Export to structured JSON for compliance.</summary>
    string ExportToJson(int maxCount = 1000, DateTimeOffset? since = null);

    /// <summary>Export to human-readable Markdown.</summary>
    string ExportToMarkdown(int maxCount = 1000, DateTimeOffset? since = null);
}

/// <summary>
/// DTO for sanitization audit entries (avoids dependency on BackgroundAgents).
/// </summary>
public sealed record SanitizationAuditEntryDto(
    DateTimeOffset Timestamp,
    string RuleVersion,
    string FieldOrType,
    string Disposition,
    string? Reason);
