namespace Ashlar.Core.Application.Workflows;

/// <summary>Per-node execution metrics.</summary>
public class NodeMetrics
{
    /// <summary>Wall-clock duration for this node.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Brick implementation selected for this node, when applicable.</summary>
    public Ashlar.Core.Domain.Bricks.ImplementationType? Implementation { get; init; }

    /// <summary>LLM tokens consumed by this node, when applicable.</summary>
    public long? TokensUsed { get; init; }

    /// <summary>Whether a semantic cache hit avoided an LLM call.</summary>
    public bool CacheHit { get; init; }
}
