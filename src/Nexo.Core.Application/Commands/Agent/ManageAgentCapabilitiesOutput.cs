using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Output for managing agent capabilities
    /// </summary>
    public class ManageAgentCapabilitiesOutput
    {
        public string AgentId { get; set; } = string.Empty;
        public CapabilityAction Action { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> FocusAreas { get; set; } = new List<string>();
        public List<string> Capabilities { get; set; } = new List<string>();
    }
}
