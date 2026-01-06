namespace Nexo.Core.Application.Agent.Models;

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
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}

