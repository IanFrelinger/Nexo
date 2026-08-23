namespace Ashlar.Core.Application.Agent.Models;

/// <summary>
/// Metadata about an agent.
/// 
/// Contains:
/// - Agent name and description
/// - List of capabilities
/// - Dictionary of parameters
/// 
/// Used by IAgentRegistry to provide information about available agents.
/// Returned by CLI commands like "list agents".
/// </summary>
public record AgentMetadata
{
    /// <summary>Registered agent name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; init; }

    /// <summary>Capability identifiers advertised by this agent.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();

    /// <summary>Declared parameter names and descriptions.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}

