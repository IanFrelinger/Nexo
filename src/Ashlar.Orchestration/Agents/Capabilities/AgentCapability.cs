using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Agents.Capabilities;

/// <summary>
/// Capabilities of an agent.
/// 
/// Contains:
/// - Agent ID and domain
/// - List of capabilities (e.g., "image_generation", "code_analysis")
/// - Supported input modalities (text, image, audio)
/// - Supported programming languages
/// - Whether the agent supports learning
/// - Optional metadata dictionary
/// 
/// Used by AgentCapabilityRegistry to track agent capabilities.
/// </summary>
public sealed record AgentCapability
{
    /// <summary>Unique agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Agent domain or specialty.</summary>
    public required string Domain { get; init; }

    /// <summary>Named capabilities the agent exposes.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = new List<string>();

    /// <summary>Input modalities the agent accepts.</summary>
    public IReadOnlyList<string> SupportedModalities { get; init; } = new List<string> { "text" };

    /// <summary>Programming languages the agent supports.</summary>
    public IReadOnlyList<string> SupportedLanguages { get; init; } = new List<string>();

    /// <summary>Whether the agent can learn from feedback.</summary>
    public bool SupportsLearning { get; init; }

    /// <summary>Optional capability metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
