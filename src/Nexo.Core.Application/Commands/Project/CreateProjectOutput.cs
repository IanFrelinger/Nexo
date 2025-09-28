using System;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Output for creating a project
    /// </summary>
    public class CreateProjectOutput
    {
        public Nexo.Core.Domain.Entities.Project Project { get; set; } = ProjectFactory.CreateDefaultProject();
        public string ProjectId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
