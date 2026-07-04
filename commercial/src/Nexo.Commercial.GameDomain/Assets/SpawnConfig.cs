namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Spawn config record.</summary>
public sealed record SpawnConfig
{
    /// <summary>Auto spawn value.</summary>
    public bool AutoSpawn { get; init; } = true;
    /// <summary>Spawn authority value.</summary>
    public string SpawnAuthority { get; init; } = "server"; // server, owner
    /// <summary>Despawn delay value.</summary>
    public double DespawnDelay { get; init; } = 0.0;
    /// <summary>pool id value.</summary>
    public string? PoolId { get; init; } // null = no pooling, string = pool group name
}
