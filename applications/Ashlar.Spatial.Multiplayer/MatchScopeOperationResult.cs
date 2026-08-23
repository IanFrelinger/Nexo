namespace Ashlar.Spatial.Multiplayer;

/// <summary>
/// Result of a match scope mutation.
/// </summary>
public sealed class MatchScopeOperationResult
{
    public MatchScopeOperationResult(bool succeeded, MatchScope? scope, string? rejectionCode, string? reason)
    {
        Succeeded = succeeded;
        Scope = scope;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    /// <summary>Whether the scope operation completed successfully.</summary>
    public bool Succeeded { get; }

    /// <summary>Updated scope state when <see cref="Succeeded"/> is true.</summary>
    public MatchScope? Scope { get; }

    /// <summary>Machine-readable rejection code when <see cref="Succeeded"/> is false.</summary>
    public string? RejectionCode { get; }

    /// <summary>Human-readable rejection detail when <see cref="Succeeded"/> is false.</summary>
    public string? Reason { get; }

    /// <summary>Creates a successful scope operation result.</summary>
    public static MatchScopeOperationResult Success(MatchScope scope) => new(true, scope, null, null);

    /// <summary>Creates a rejected scope operation result.</summary>
    public static MatchScopeOperationResult Rejected(string rejectionCode, string reason) =>
        new(false, null, rejectionCode, reason);
}
