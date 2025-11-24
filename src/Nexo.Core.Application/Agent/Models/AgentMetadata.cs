namespace Nexo.Core.Application.Agent.Models;

/// <summary>
/// Metadata about an agent.
/// </summary>
public record AgentMetadata
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}

