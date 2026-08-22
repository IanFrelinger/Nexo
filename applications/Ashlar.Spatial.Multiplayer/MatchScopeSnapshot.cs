using Ashlar.Spatial.Contracts;

namespace Ashlar.Spatial.Multiplayer;

/// <summary>
/// Scope snapshot for late joiners — membership and scoped atoms only, no historical poses.
/// </summary>
public sealed record MatchScopeSnapshot(
    string ScopeId,
    string HostParticipantId,
    IReadOnlyCollection<string> MemberParticipantIds,
    IReadOnlyCollection<string> ScopedAtomIds);
