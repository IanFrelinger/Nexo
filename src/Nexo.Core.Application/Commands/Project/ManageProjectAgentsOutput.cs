using System.Collections.Generic;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Output for managing project agents
    /// </summary>
    public class ManageProjectAgentsOutput
    {
        public string ProjectId { get; set; } = string.Empty;
        public AgentAction Action { get; set; }
        public List<Nexo.Core.Domain.Entities.Agent> Agents { get; set; } = new List<Nexo.Core.Domain.Entities.Agent>();
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
