using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.Orchestration.Agents;
using Ashlar.Runtime;

namespace Ashlar.BackgroundAgents.Tools;

/// <summary>
/// IToolbox that contains only background agent management tools (enable, disable, restart, update config).
/// Used by the meta-agent or any host that needs to manage background agents via tools.
/// </summary>
public sealed class AgentManagementToolbox : IToolbox
{
    private readonly CapabilityRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentManagementToolbox"/> class
    /// and registers enable_agent, disable_agent, restart_agent, update_agent_config.
    /// </summary>
    /// <param name="registry">Background agent registry.</param>
    /// <param name="configLoader">Config loader (for update_agent_config).</param>
    /// <param name="specBuilder">Spec builder (for update_agent_config).</param>
    /// <param name="agentFactory">Agent factory (for update_agent_config).</param>
    public AgentManagementToolbox(
        IBackgroundAgentRegistry registry,
        BackgroundAgentConfigLoader configLoader,
        BackgroundAgentSpecBuilder specBuilder,
        AgentFactory agentFactory)
    {
        _registry = new CapabilityRegistry();
        _registry.Register(new EnableAgentTool(registry));
        _registry.Register(new DisableAgentTool(registry));
        _registry.Register(new RestartAgentTool(registry));
        _registry.Register(new UpdateAgentConfigTool(registry, configLoader, specBuilder, agentFactory));
    }

    /// <inheritdoc />
    public IEnumerable<ToolSchema> Schemas() => _registry.Schemas();

    /// <inheritdoc />
    public Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
        => _registry.InvokeAsync(toolCall, s, ct);

    /// <inheritdoc />
    public IAgentMemory MemoryFor(IAgent agent) => _registry.MemoryFor(agent);
}
