namespace Nexo.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// A single recorded brick execution for usage tracking (synaptic plasticity).
/// </summary>
public sealed record BrickUsageRecord
{
    /// <summary>Brick identifier.</summary>
    public required string BrickId { get; init; }

    /// <summary>Node (instance) that executed the brick.</summary>
    public required string NodeId { get; init; }

    /// <summary>When the execution occurred.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Execution duration in milliseconds.</summary>
    public required long DurationMs { get; init; }

    /// <summary>Whether the execution succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Implementation used: "Deterministic" or "Agentic".</summary>
    public required string Implementation { get; init; }

    /// <summary>Whether the brick was executed on a remote host.</summary>
    public required bool IsRemote { get; init; }
}
