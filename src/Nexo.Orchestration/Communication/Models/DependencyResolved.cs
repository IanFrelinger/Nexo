using System.Text.Json;

namespace Nexo.Orchestration.Communication.Models;

/// <summary>
/// Message emitted when a dependency is resolved.
/// 
/// Contains the dependency agent ID and its output.
/// Used to notify waiting agents that their dependencies are ready.
/// </summary>
public sealed record DependencyResolved : AgentMessage
{
    public DependencyResolved()
    {
        MessageType = "DependencyResolved";
    }

    /// <summary>
    /// ID of the agent that resolved the dependency.
    /// </summary>
    public required string DependencyAgentId { get; init; }

    /// <summary>
    /// The resolved dependency output.
    /// </summary>
    public required object DependencyOutput { get; init; }
}
