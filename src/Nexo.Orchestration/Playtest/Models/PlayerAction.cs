namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// An action taken by an AI player.
/// </summary>
public sealed record PlayerAction
{
    /// <summary>Action type identifier.</summary>
    public required string ActionType { get; init; }

    /// <summary>UTC timestamp when the action occurred.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Action-specific parameters.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Optional AI reasoning for the action.</summary>
    public string? Reasoning { get; init; }
}
