using Ashlar.Core.Application.Trust.Models;

namespace Ashlar.Core.Application.Trust.Ports;

/// <summary>
/// DTO for sanitization audit entries (avoids dependency on BackgroundAgents).
/// </summary>
/// <param name="Timestamp">When the sanitization occurred.</param>
/// <param name="RuleVersion">Sanitization rule version applied.</param>
/// <param name="FieldOrType">Field or data type sanitized.</param>
/// <param name="Disposition">Outcome disposition (redacted, dropped, etc.).</param>
/// <param name="Reason">Optional reason for the disposition.</param>
public sealed record SanitizationAuditEntryDto(
    DateTimeOffset Timestamp,
    string RuleVersion,
    string FieldOrType,
    string Disposition,
    string? Reason);
