using Nexo.Core.Domain.Agents;

namespace Nexo.Demo.Api.DTOs;

/// <summary>
/// Data transfer object for AgentCard.
/// </summary>
public class AgentDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = default!;
    public string Icon { get; init; } = default!;
    public string Domain { get; init; } = default!;
    public string Description { get; init; } = default!;
    public IReadOnlyList<string> Platforms { get; init; } = [];
    public IReadOnlyList<string> Behaviors { get; init; } = [];
    public AgentMemoryConfigDto Memory { get; init; } = new();
    
    public static AgentDto FromDomain(AgentCard agent)
    {
        return new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Version = agent.Version,
            Icon = agent.Icon,
            Domain = agent.Domain,
            Description = agent.Description,
            Platforms = agent.Platforms.Select(p => p.ToString().ToLowerInvariant()).ToList(),
            Behaviors = agent.Behaviors,
            Memory = new AgentMemoryConfigDto
            {
                SemanticCache = agent.Memory.SemanticCache,
                PatternLibrary = agent.Memory.PatternLibrary,
                CrossProjectSharing = agent.Memory.CrossProjectSharing
            }
        };
    }
}

public class AgentMemoryConfigDto
{
    public bool SemanticCache { get; init; }
    public bool PatternLibrary { get; init; }
    public bool CrossProjectSharing { get; init; }
}

