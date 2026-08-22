namespace Ashlar.BackgroundAgents.Trust;

/// <summary>
/// Result of sanitization: allowed with sanitized context, or blocked.
/// </summary>
public sealed class SanitizationResult
{
    /// <summary>Whether the context is safe to send.</summary>
    public bool Allowed { get; init; }

    /// <summary>Sanitized context (when Allowed).</summary>
    public OutgoingContext? SanitizedContext { get; init; }

    /// <summary>Reason when blocked.</summary>
    public string? BlockReason { get; init; }

    /// <summary>Audit entries for any redactions applied.</summary>
    public IReadOnlyList<SanitizationAuditEntry> Redactions { get; init; } = Array.Empty<SanitizationAuditEntry>();

    public static SanitizationResult AllowedWith(OutgoingContext ctx, IReadOnlyList<SanitizationAuditEntry>? redactions = null) =>
        new()
        {
            Allowed = true,
            SanitizedContext = ctx,
            Redactions = redactions ?? Array.Empty<SanitizationAuditEntry>(),
        };

    public static SanitizationResult Blocked(string reason) =>
        new()
        {
            Allowed = false,
            BlockReason = reason,
        };
}
