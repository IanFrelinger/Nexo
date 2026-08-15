namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// Session-lifetime scope aggregate. Not a durable trust artifact — lookup structure only.
/// </summary>
public sealed record MatchScope
{
    /// <summary>Unique scope identifier for the match session.</summary>
    public string ScopeId { get; init; } = string.Empty;

    /// <summary>Participant id of the scope host authorized to publish poses.</summary>
    public string HostParticipantId { get; init; } = string.Empty;

    /// <summary>Participant ids currently joined to the scope.</summary>
    public IReadOnlyCollection<string> MemberParticipantIds { get; init; } = EmptyMembers;

    /// <summary>Certified atom ids included in this scope.</summary>
    public IReadOnlyCollection<string> ScopedAtomIds { get; init; } = EmptyMembers;

    private static readonly IReadOnlyCollection<string> EmptyMembers = Array.Empty<string>();
}
