using System.Collections.Generic;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Input for creating an agent
    /// </summary>
    public class CreateAgentInput
    {
        public string Name { get; set; } = string.Empty;
        public AgentRole? Role { get; set; }
        public List<string>? FocusAreas { get; set; }
        public List<string>? Capabilities { get; set; }
        public Dictionary<string, object>? Configuration { get; set; }
        public string? Description { get; set; }
    }
}
