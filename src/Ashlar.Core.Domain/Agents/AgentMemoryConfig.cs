namespace Ashlar.Core.Domain.Agents;

/// <summary>
/// Memory configuration for an agent.
/// </summary>
public class AgentMemoryConfig
{
    /// <summary>Enable semantic caching of prior LLM completions.</summary>
    public bool SemanticCache { get; init; }

    /// <summary>Enable persisted pattern library for cross-run learning.</summary>
    public bool PatternLibrary { get; init; }

    /// <summary>Allow memory artifacts to be shared across projects.</summary>
    public bool CrossProjectSharing { get; init; }
}
