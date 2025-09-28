using System.Collections.Generic;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Result of project creation
    /// </summary>
    public class ProjectCreationResult
    {
        public bool IsSuccess { get; set; }
        public Nexo.Core.Domain.Entities.Project? Project { get; set; }
        public string? ProjectId { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
