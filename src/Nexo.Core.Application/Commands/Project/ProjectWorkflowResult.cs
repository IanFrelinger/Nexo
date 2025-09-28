using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Result of project workflow execution
    /// </summary>
    public class ProjectWorkflowResult
    {
        public bool IsSuccess { get; set; }
        public List<object> Results { get; set; } = new List<object>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
