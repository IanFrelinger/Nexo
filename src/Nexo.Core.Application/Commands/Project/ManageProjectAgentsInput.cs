using System;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Input for managing project agents
    /// </summary>
    public class ManageProjectAgentsInput
    {
        public string ProjectId { get; set; } = string.Empty;
        public Nexo.Core.Domain.Entities.Project Project { get; set; } = ProjectFactory.CreateDefaultProject();
        public AgentAction Action { get; set; }
        public Nexo.Core.Domain.Entities.Agent? Agent { get; set; }
        public string? AgentId { get; set; }
    }
}
