using System.Collections.Generic;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Input for project workflow execution
    /// </summary>
    public class ProjectWorkflowInput
    {
        public bool CreateProject { get; set; } = true;
        public CreateProjectInput CreateInput { get; set; } = new CreateProjectInput();
        public Nexo.Core.Domain.Entities.Project? Project { get; set; }
        public List<Nexo.Core.Domain.Entities.Agent>? AgentsToAdd { get; set; }
        public bool CalculateMetrics { get; set; } = true;
    }
}
