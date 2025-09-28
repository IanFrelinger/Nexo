using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Command to create a new project
    /// </summary>
    public class CreateProjectCommand : BaseCommand<CreateProjectInput, CreateProjectOutput>
    {
        public override string Id => "create-project";
        public override string Name => "Create Project";
        public override string Description => "Creates a new project with initial configuration";

        protected override async Task<CreateProjectOutput> ExecuteInternalAsync(CreateProjectInput input, CancellationToken cancellationToken)
        {
            // Simulate project creation
            await Task.Delay(100, cancellationToken);

            return new CreateProjectOutput
            {
                ProjectId = Guid.NewGuid().ToString(),
                ProjectName = input.Name,
                ProjectPath = input.Path,
                CreatedAt = DateTime.UtcNow,
                Status = ProjectCreationStatus.Success,
                Configuration = input.Metadata ?? new Dictionary<string, object>(),
                Agents = new List<AgentInfo>()
            };
        }
    }

    /// <summary>
    /// Input for creating a project
    /// </summary>
    public class CreateProjectInput
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ContainerRuntime? Runtime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Output from project creation
    /// </summary>
    public class CreateProjectOutput
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ProjectCreationStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public IReadOnlyList<AgentInfo> Agents { get; set; } = new List<AgentInfo>();
    }

    /// <summary>
    /// Project creation status
    /// </summary>
    public enum ProjectCreationStatus
    {
        Success,
        Failed,
        Partial
    }

    /// <summary>
    /// Information about an agent in a project
    /// </summary>
    public class AgentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
