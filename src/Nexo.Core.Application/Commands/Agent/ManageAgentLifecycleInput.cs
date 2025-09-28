using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Input for managing agent lifecycle
    /// </summary>
    public class ManageAgentLifecycleInput
    {
        public string AgentId { get; set; } = string.Empty;
        public Nexo.Core.Domain.Entities.Agent Agent { get; set; } = AgentFactory.CreateDefaultAgent();
        public AgentLifecycleAction Action { get; set; }
        public string? FailureReason { get; set; }
    }
}
