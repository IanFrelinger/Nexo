using Ashlar.Core.Application.Trust.Models;

namespace Ashlar.Core.Application.Trust.Ports;

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

    /// <summary>Log a scope chain rejection (adversarial escape blocked).</summary>
    /// <param name="chainIds">Adaptation/call IDs in the chain.</param>
    /// <param name="rejectedStep">Index at which boundary was crossed.</param>
    /// <param name="resolvedPath">Path that triggered rejection.</param>
    /// <param name="reason">Rejection reason.</param>
    void LogScopeChainRejection(IReadOnlyList<string> chainIds, int rejectedStep, string? resolvedPath, string reason);

    /// <summary>Log an Ambient mode action (silent execution, no user notification).</summary>
    /// <param name="agentId">Background agent ID.</param>
    /// <param name="summary">Action summary.</param>
    /// <param name="toolCallsExecuted">Number of tool calls executed.</param>
    void LogAmbientAction(string agentId, string summary, int toolCallsExecuted);

    /// <summary>Log a copilot task completion for tenant-scoped audit trails (Product Fleet Phase 0.5).</summary>
    void LogCopilotTask(string tenantId, string taskId, bool success);

    /// <summary>Get recent audit entries (all types).</summary>
    IReadOnlyList<DataDecisionAuditEntry> GetRecent(int maxCount, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null);

    /// <summary>Export to structured JSON for compliance.</summary>
    string ExportToJson(int maxCount = 1000, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null);

    /// <summary>Export to human-readable Markdown.</summary>
    string ExportToMarkdown(int maxCount = 1000, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null);

    /// <summary>Export to CSV for compliance.</summary>
    string ExportToCsv(int maxCount = 1000, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null);
}
