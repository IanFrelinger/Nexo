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
