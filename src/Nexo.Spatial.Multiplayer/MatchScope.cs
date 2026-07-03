namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// Session-lifetime scope aggregate. Not a durable trust artifact — lookup structure only.
/// </summary>
public sealed record MatchScope
{
    public string ScopeId { get; init; } = string.Empty;

    public string HostParticipantId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> MemberParticipantIds { get; init; } = EmptyMembers;

    public IReadOnlyCollection<string> ScopedAtomIds { get; init; } = EmptyMembers;

    private static readonly IReadOnlyCollection<string> EmptyMembers = Array.Empty<string>();
}
