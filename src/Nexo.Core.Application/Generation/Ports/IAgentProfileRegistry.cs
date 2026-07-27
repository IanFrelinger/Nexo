using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Core.Application.Generation.Ports;

/// <summary>Resolves registered <see cref="AgentProfile"/> instances by target id.</summary>
public interface IAgentProfileRegistry
{
    /// <summary>Returns the profile for <paramref name="targetId"/>, or null if unknown.</summary>
    AgentProfile? Resolve(string targetId);

    /// <summary>Registers or replaces a profile.</summary>
    void Register(AgentProfile profile);

    /// <summary>All registered target ids.</summary>
    IReadOnlyCollection<string> TargetIds { get; }
}
