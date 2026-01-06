namespace Nexo.Core.Domain.Agents;

/// <summary>
/// An Agent Card is a cross-platform AI persona with identity, memory, and behaviors.
/// </summary>
public class AgentCard
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = "1.0.0";
    public string Icon { get; init; } = "🤖";
    public string Domain { get; init; } = default!;
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

/// <summary>
/// Platforms supported by agents.
/// </summary>
public enum Platform
{
    Unity,
    Web,
    Cli,
    Api,
    VsCode,
    Slack,
    Teams,
    Discord
}

/// <summary>
/// Platform-specific configuration for an agent.
/// </summary>
public class PlatformConfig
{
    public string? UiComponent { get; init; }
    public string EntryPoint { get; init; } = default!;
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}

/// <summary>
/// Memory configuration for an agent.
/// </summary>
public class AgentMemoryConfig
{
    public bool SemanticCache { get; init; }
    public bool PatternLibrary { get; init; }
    public bool CrossProjectSharing { get; init; }
}

/// <summary>
/// Operational constraints for an agent.
/// </summary>
public class AgentConstraints
{
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentBehaviors { get; init; } = 3;
    public IReadOnlyList<EscalationRule> EscalationRules { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

/// <summary>
/// Rule for escalating agent actions.
/// </summary>
public class EscalationRule
{
    public string Condition { get; init; } = default!;
    public string Action { get; init; } = default!;
    public string? Target { get; init; }
    public string? Reason { get; init; }
    
    public EscalationRule(string condition, string action, string? target = null, string? reason = null)
    {
        Condition = condition;
        Action = action;
        Target = target;
        Reason = reason;
    }
}

