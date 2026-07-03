namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// In-memory match scope lifecycle store (transport-agnostic).
/// </summary>
public interface IMatchScopeStore
{
    MatchScopeOperationResult TryCreate(string scopeId, string hostParticipantId, IEnumerable<string> scopedAtomIds);

    MatchScopeOperationResult TryJoin(string scopeId, string participantId);

    MatchScopeOperationResult TryLeave(string scopeId, string participantId);

    void End(string scopeId);

    MatchScope? GetScope(string scopeId);
}

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

    public bool Succeeded { get; }

    public MatchScope? Scope { get; }

    public string? RejectionCode { get; }

    public string? Reason { get; }

    public static MatchScopeOperationResult Success(MatchScope scope) => new(true, scope, null, null);

    public static MatchScopeOperationResult Rejected(string rejectionCode, string reason) =>
        new(false, null, rejectionCode, reason);
}
