namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// In-memory match scope store with first-writer-wins host claim.
/// </summary>
public sealed class InMemoryMatchScopeStore : IMatchScopeStore
{
    private readonly Dictionary<string, ScopeState> _scopes = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public MatchScopeOperationResult TryCreate(string scopeId, string hostParticipantId, IEnumerable<string> scopedAtomIds)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Scope id is required.", nameof(scopeId));
        if (string.IsNullOrWhiteSpace(hostParticipantId))
            throw new ArgumentException("Host participant id is required.", nameof(hostParticipantId));
        if (scopedAtomIds is null)
            throw new ArgumentNullException(nameof(scopedAtomIds));

        lock (_gate)
        {
            if (_scopes.TryGetValue(scopeId, out var existing))
            {
                if (string.Equals(existing.Scope.HostParticipantId, hostParticipantId, StringComparison.Ordinal))
                    return MatchScopeOperationResult.Success(existing.Scope);

                return MatchScopeOperationResult.Rejected(
                    "host-already-claimed",
                    $"Scope '{scopeId}' host already claimed by '{existing.Scope.HostParticipantId}'.");
            }

            var atomSet = new HashSet<string>(
                scopedAtomIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.Ordinal);
            var members = new HashSet<string>(StringComparer.Ordinal) { hostParticipantId };
            var scope = new MatchScope
            {
                ScopeId = scopeId,
                HostParticipantId = hostParticipantId,
                MemberParticipantIds = members,
                ScopedAtomIds = atomSet
            };

            _scopes[scopeId] = new ScopeState(scope);
            return MatchScopeOperationResult.Success(scope);
        }
    }

    public MatchScopeOperationResult TryJoin(string scopeId, string participantId)
    {
        lock (_gate)
        {
            if (!_scopes.TryGetValue(scopeId, out var state))
            {
                return MatchScopeOperationResult.Rejected("scope-not-found", $"Scope '{scopeId}' does not exist.");
            }

            var members = new HashSet<string>(state.Scope.MemberParticipantIds, StringComparer.Ordinal);
            members.Add(participantId);
            state.Scope = state.Scope with { MemberParticipantIds = members };
            return MatchScopeOperationResult.Success(state.Scope);
        }
    }

    public MatchScopeOperationResult TryLeave(string scopeId, string participantId)
    {
        lock (_gate)
        {
            if (!_scopes.TryGetValue(scopeId, out var state))
            {
                return MatchScopeOperationResult.Rejected("scope-not-found", $"Scope '{scopeId}' does not exist.");
            }

            var members = new HashSet<string>(state.Scope.MemberParticipantIds, StringComparer.Ordinal);
            members.Remove(participantId);
            state.Scope = state.Scope with { MemberParticipantIds = members };
            return MatchScopeOperationResult.Success(state.Scope);
        }
    }

    public void End(string scopeId)
    {
        lock (_gate)
        {
            _scopes.Remove(scopeId);
        }
    }

    public MatchScope? GetScope(string scopeId)
    {
        lock (_gate)
        {
            return _scopes.TryGetValue(scopeId, out var state) ? state.Scope : null;
        }
    }

    private sealed class ScopeState(MatchScope scope)
    {
        public MatchScope Scope { get; set; } = scope;
    }
}
