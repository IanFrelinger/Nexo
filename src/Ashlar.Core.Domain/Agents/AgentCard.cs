namespace Ashlar.Core.Domain.Agents;

/// <summary>
/// An Agent Card is a cross-platform AI persona with identity, memory, and behaviors.
/// </summary>
public class AgentCard
{
    /// <summary>Stable agent identifier used in registries and routing.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable agent display name.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Semantic version of the agent card definition.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Emoji or icon token for operator UIs.</summary>
    public string Icon { get; init; } = "🤖";

    /// <summary>Primary domain or specialty label (e.g. security, codegen).</summary>
    public string Domain { get; init; } = default!;

    /// <summary>Short description of the agent's purpose.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Platforms this agent supports.
    /// </summary>
    public IReadOnlyList<Platform> Platforms { get; init; } = [];
    
    /// <summary>
    /// Platform-specific configurations.
    /// </summary>
    public IReadOnlyDictionary<Platform, PlatformConfig> PlatformConfigs { get; init; } = 
        new Dictionary<Platform, PlatformConfig>();
    
    /// <summary>
    /// Behaviors this agent can execute.
    /// </summary>
    public IReadOnlyList<string> Behaviors { get; init; } = [];
    
    /// <summary>
    /// Memory configuration.
    /// </summary>
    public AgentMemoryConfig Memory { get; init; } = new();
    
    /// <summary>
    /// Operational constraints.
    /// </summary>
    public AgentConstraints Constraints { get; init; } = new();
    
    /// <summary>
    /// Other agents this agent can coordinate with.
    /// </summary>
    public IReadOnlyList<string> CanCoordinateWith { get; init; } = [];
    
    /// <summary>
    /// Supported coordination protocols.
    /// </summary>
    public IReadOnlyList<string> CoordinationProtocols { get; init; } = [];
}
