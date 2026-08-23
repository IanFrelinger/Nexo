namespace Ashlar.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Multiplayer synchronization rules injected into generation prompts.</summary>
public sealed record NetworkingConstraints
{
    /// <summary>Network authority model (e.g. server, client, shared).</summary>
    public string? Authority { get; init; }

    /// <summary>Maximum networked variables allowed per object.</summary>
    public int MaxSyncedVariablesPerObject { get; init; }

    /// <summary>When true, mutating actions must route through server RPCs.</summary>
    public bool RequireServerRpc { get; init; }
}
