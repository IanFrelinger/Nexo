namespace Ashlar.AI.Pipeline.Governance;

/// <summary>
/// Thrown when policy denies a model invocation or sanitization blocks the request.
/// </summary>
public sealed class PolicyViolationException : Exception
{
    /// <summary>Creates a structured policy violation.</summary>
    public PolicyViolationException(
        string code,
        string message,
        string? targetKey = null,
        string? modelId = null,
        IReadOnlyDictionary<string, string>? details = null)
        : base(message)
    {
        Code = code;
        TargetKey = targetKey;
        ModelId = modelId;
        Details = details ?? new Dictionary<string, string>();
    }

    /// <summary>Machine-readable reason code (e.g. <c>target_denied</c>, <c>pii_blocked</c>).</summary>
    public string Code { get; }

    /// <summary>Target key that was evaluated, when known.</summary>
    public string? TargetKey { get; }

    /// <summary>Model id that was evaluated, when known.</summary>
    public string? ModelId { get; }

    /// <summary>Additional structured details (never contains raw secret/PII content).</summary>
    public IReadOnlyDictionary<string, string> Details { get; }
}
