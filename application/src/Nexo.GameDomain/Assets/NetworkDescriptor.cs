namespace Nexo.GameDomain.Assets;

/// <summary>
/// Network object descriptor for Unity Netcode for GameObjects. Defines
/// which components replicate, synchronization mode, ownership rules,
/// and RPC patterns for multiplayer FPS gameplay.
/// </summary>
public sealed record NetworkDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "player"; // player, projectile, pickup, game_state, environment

    public bool IsPlayerObject { get; init; }
    public bool DontDestroyWithOwner { get; init; }

    public IReadOnlyList<NetworkVariable> SyncedVariables { get; init; } = Array.Empty<NetworkVariable>();
    public IReadOnlyList<NetworkRpc> Rpcs { get; init; } = Array.Empty<NetworkRpc>();
    public SpawnConfig Spawning { get; init; } = new();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record NetworkVariable
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "float"; // float, int, bool, Vector3, Quaternion, string, custom
    public string Permission { get; init; } = "server"; // server, owner, everyone
    public string DeliveryMode { get; init; } = "reliable"; // reliable, unreliable
    public double SendRate { get; init; } = 0.0; // 0 = every change, >0 = max sends per second
}

public sealed record NetworkRpc
{
    public string Name { get; init; } = string.Empty;
    public string Direction { get; init; } = "server"; // server (client→server), client (server→client)
    public string DeliveryMode { get; init; } = "reliable";
    public IReadOnlyList<RpcParameter> Parameters { get; init; } = Array.Empty<RpcParameter>();
}

public sealed record RpcParameter
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "float";
}

public sealed record SpawnConfig
{
    public bool AutoSpawn { get; init; } = true;
    public string SpawnAuthority { get; init; } = "server"; // server, owner
    public double DespawnDelay { get; init; } = 0.0;
    public string? PoolId { get; init; } // null = no pooling, string = pool group name
}
