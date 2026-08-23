namespace Ashlar.Orchestration.Agents;

/// <summary>Internal helpers for unwrapping in-process agent handles.</summary>
internal static class AgentHandleExtensions
{
    internal static AgentContainer? TryGetInProcessContainer(this Ashlar.Abstractions.Agents.IAgentHandle handle) =>
        handle is InProcessAgentHandle inProcess ? inProcess.Container : null;
}
