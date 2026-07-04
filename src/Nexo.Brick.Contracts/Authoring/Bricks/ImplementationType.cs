namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Types of implementations available for a brick.
/// </summary>
public enum ImplementationType
{
    /// <summary>Rule-based or pattern-matching execution path.</summary>
    Deterministic,

    /// <summary>LLM-backed reasoning execution path.</summary>
    Agentic,

    /// <summary>Resolved at runtime via <see cref="ImplementationSelector"/> or host defaults.</summary>
    Auto
}
