namespace Ashlar.AI.Pipeline.Governance;

/// <summary>
/// Per-target governance settings resolved for sanitization middleware.
/// </summary>
public interface ITargetSanitizePolicy
{
    /// <summary>Resolves the sanitize disposition for a target key.</summary>
    SanitizeDisposition GetDisposition(string targetKey);
}

/// <summary>
/// Default dispositions: local pass-through; cloud block-on-secret / redact-on-PII.
/// </summary>
public sealed class DefaultTargetSanitizePolicy : ITargetSanitizePolicy
{
    /// <inheritdoc />
    public SanitizeDisposition GetDisposition(string targetKey)
    {
        if (targetKey.StartsWith("cloud:", StringComparison.OrdinalIgnoreCase))
        {
            return SanitizeDisposition.BlockOnSecretRedactOnPii;
        }

        // local:* and peer:* — pass by default (policy packs can override later).
        return SanitizeDisposition.Pass;
    }
}
