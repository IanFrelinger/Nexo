using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Output for managing agent lifecycle
    /// </summary>
    public class ManageAgentLifecycleOutput
    {
        public string AgentId { get; set; } = string.Empty;
        public AgentLifecycleAction Action { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public AgentStatus NewStatus { get; set; } = AgentStatus.Inactive;
    }
}
