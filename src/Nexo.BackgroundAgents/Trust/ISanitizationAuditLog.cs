namespace Nexo.BackgroundAgents.Trust;

/// <summary>
/// Log of sanitization events (what was stripped, why).
/// </summary>
public interface ISanitizationAuditLog
{
    /// <summary>
    /// Logs a redaction event.
    /// </summary>
    void LogRedaction(DateTimeOffset timestamp, string ruleVersion, string fieldOrType, string disposition, string? reason);

    /// <summary>
    /// Gets recent audit entries.
    /// </summary>
    IReadOnlyList<SanitizationAuditEntry> GetRecent(int maxCount, DateTimeOffset? since = null);
}
