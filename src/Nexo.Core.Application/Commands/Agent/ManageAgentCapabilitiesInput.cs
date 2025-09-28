using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Input for managing agent capabilities
    /// </summary>
    public class ManageAgentCapabilitiesInput
    {
        public string AgentId { get; set; } = string.Empty;
        public Nexo.Core.Domain.Entities.Agent Agent { get; set; } = AgentFactory.CreateDefaultAgent();
        public CapabilityAction Action { get; set; }
        public string? Item { get; set; }
    }
}
