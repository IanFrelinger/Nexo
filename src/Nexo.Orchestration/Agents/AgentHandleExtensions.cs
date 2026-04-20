namespace Nexo.Orchestration.Agents;

internal static class AgentHandleExtensions
{
    internal static AgentContainer? TryGetInProcessContainer(this Nexo.Abstractions.Agents.IAgentHandle handle) =>
        handle is InProcessAgentHandle inProcess ? inProcess.Container : null;
}
