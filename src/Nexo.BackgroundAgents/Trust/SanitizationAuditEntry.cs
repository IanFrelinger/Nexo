namespace Nexo.BackgroundAgents.Trust;

/// <summary>
/// Single audit entry for a sanitization action.
/// </summary>
public sealed class SanitizationAuditEntry
{
    /// <summary>When the action occurred.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Rule version or identifier.</summary>
    public string RuleVersion { get; init; } = string.Empty;

    /// <summary>Field or data type affected.</summary>
    public string FieldOrType { get; init; } = string.Empty;

    /// <summary>Disposition: stripped, blocked, allowed, redacted.</summary>
    public string Disposition { get; init; } = string.Empty;

    /// <summary>Optional reason.</summary>
    public string? Reason { get; init; }
}
