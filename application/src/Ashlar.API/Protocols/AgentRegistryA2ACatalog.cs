using Ashlar.Core.Domain.Execution;
using Ashlar.Transport.A2A.Server;

namespace Ashlar.API.Protocols;

/// <summary>
/// Host-side implementation of the A2A adapter's catalog port over the kernel's
/// <see cref="IAgentRegistry"/>. Lives in the application layer because the transport adapter
/// deliberately cannot reference <c>Ashlar.Core.Domain</c> (transport layering rules) — this is
/// the one place the domain persona model and the A2A descriptor meet.
/// </summary>
public sealed class AgentRegistryA2ACatalog : IAshlarA2AAgentCatalog
{
    private readonly IAgentRegistry _registry;

    /// <summary>Creates the catalog over the kernel agent registry.</summary>
    public AgentRegistryA2ACatalog(IAgentRegistry registry) => _registry = registry;

    /// <inheritdoc />
    public IReadOnlyList<AshlarA2AAgentDescriptor> GetAgents()
    {
        var agents = _registry.GetAllAgents();
        var descriptors = new List<AshlarA2AAgentDescriptor>(agents.Count);
        foreach (var card in agents)
        {
            descriptors.Add(new AshlarA2AAgentDescriptor(
                Id: card.Id,
                Name: card.Name,
                Description: card.Description,
                Version: card.Version,
                Domain: card.Domain,
                Behaviors: card.Behaviors,
                CoordinationProtocols: card.CoordinationProtocols,
                MaxExecutionTime: card.Constraints.MaxExecutionTime));
        }

        return descriptors;
    }
}
