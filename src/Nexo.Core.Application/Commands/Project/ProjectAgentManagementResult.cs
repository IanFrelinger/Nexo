using System.Collections.Generic;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Result of project agent management
    /// </summary>
    public class ProjectAgentManagementResult
    {
        public bool IsSuccess { get; set; }
        public List<Nexo.Core.Domain.Entities.Agent> Agents { get; set; } = new List<Nexo.Core.Domain.Entities.Agent>();
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
