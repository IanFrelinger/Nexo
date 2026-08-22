namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Network-replicated object descriptor. Defines which components replicate, synchronization mode,
/// ownership rules, and remote-call patterns for multiplayer gameplay.
/// </summary>
public sealed record NetworkDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "player"; // player, projectile, pickup, game_state, environment

    /// <summary>Whether player object.</summary>
    public bool IsPlayerObject { get; init; }
    /// <summary>Dont destroy with owner value.</summary>
    public bool DontDestroyWithOwner { get; init; }

    /// <summary>Synced variables value.</summary>
    public IReadOnlyList<NetworkVariable> SyncedVariables { get; init; } = Array.Empty<NetworkVariable>();
    /// <summary>Rpcs value.</summary>
    public IReadOnlyList<NetworkRpc> Rpcs { get; init; } = Array.Empty<NetworkRpc>();
    /// <summary>Spawning value.</summary>
    public SpawnConfig Spawning { get; init; } = new();

    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
