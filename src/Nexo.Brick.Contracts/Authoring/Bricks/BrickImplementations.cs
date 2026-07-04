namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Available implementations for a brick (deterministic and/or agentic).
/// </summary>
public class BrickImplementations
{
    /// <summary>Rule-based or pattern-matching implementation, when available.</summary>
    public DeterministicImplementation? Deterministic { get; init; }

    /// <summary>LLM-backed implementation, when available.</summary>
    public AgenticImplementation? Agentic { get; init; }

    /// <summary>Whether a deterministic implementation is registered.</summary>
    public bool HasDeterministic => Deterministic is not null;

    /// <summary>Whether an agentic implementation is registered.</summary>
    public bool HasAgentic => Agentic is not null;
}
