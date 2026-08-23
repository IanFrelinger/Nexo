namespace Ashlar.AI.Pipeline.Governance;

/// <summary>
/// How outbound content is treated for a given target.
/// </summary>
public enum SanitizeDisposition
{
    /// <summary>Do not alter outbound content (typical for air-gapped/local-only targets).</summary>
    Pass = 0,

    /// <summary>Redact PII/secrets and continue.</summary>
    Redact = 1,

    /// <summary>Block the call when secrets are found; redact PII otherwise (strict cloud default).</summary>
    BlockOnSecretRedactOnPii = 2,

    /// <summary>Block the call when any PII or secret is found.</summary>
    BlockOnPiiOrSecret = 3,
}
